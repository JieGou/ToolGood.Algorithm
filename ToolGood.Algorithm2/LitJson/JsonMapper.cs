using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;


namespace ToolGood.Algorithm.LitJson
{
    internal struct PropertyMetadata
    {
        public MemberInfo Info;
        public bool IsField;
        public Type Type;
    }


    internal struct ArrayMetadata
    {
        private Type element_type;
        private bool is_array;
        private bool is_list;


        public Type ElementType {
            get {
                if (element_type == null)
                    return typeof(JsonData);

                return element_type;
            }

            set { element_type = value; }
        }

        public bool IsArray {
            get { return is_array; }
            set { is_array = value; }
        }

        public bool IsList {
            get { return is_list; }
            set { is_list = value; }
        }
    }


    internal struct ObjectMetadata
    {
        private Type element_type;
        private bool is_dictionary;

        private IDictionary<string, PropertyMetadata> properties;


        public Type ElementType {
            get {
                if (element_type == null)
                    return typeof(JsonData);

                return element_type;
            }

            set { element_type = value; }
        }

        public bool IsDictionary {
            get { return is_dictionary; }
            set { is_dictionary = value; }
        }

        public IDictionary<string, PropertyMetadata> Properties {
            get { return properties; }
            set { properties = value; }
        }
    }

    internal delegate object ImporterFunc(object input);

    delegate IJsonWrapper WrapperFactory();


    class JsonMapper
    {
        #region Fields
        //private static readonly int max_nesting_depth;

        private static readonly IFormatProvider datetime_format;

        private static readonly IDictionary<Type,
                IDictionary<Type, ImporterFunc>> base_importers_table;

        private static readonly IDictionary<Type, ArrayMetadata> array_metadata;
        private static readonly object array_metadata_lock = new Object();

        private static readonly IDictionary<Type,
                IDictionary<Type, MethodInfo>> conv_ops;
        private static readonly object conv_ops_lock = new Object();

        private static readonly IDictionary<Type, ObjectMetadata> object_metadata;
        private static readonly object object_metadata_lock = new Object();


        #endregion


        #region Constructors
        static JsonMapper()
        {
            //max_nesting_depth = 100;

            array_metadata = new Dictionary<Type, ArrayMetadata>();
            conv_ops = new Dictionary<Type, IDictionary<Type, MethodInfo>>();
            object_metadata = new Dictionary<Type, ObjectMetadata>();
            //type_properties = new Dictionary<Type,
            //                IList<PropertyMetadata>>();

            datetime_format = DateTimeFormatInfo.InvariantInfo;

            base_importers_table = new Dictionary<Type,
                                 IDictionary<Type, ImporterFunc>>();

            RegisterBaseImporters();
        }
        #endregion


        #region Private Methods
        private static void AddArrayMetadata(Type type)
        {
            if (array_metadata.ContainsKey(type))
                return;

            ArrayMetadata data = new ArrayMetadata {
                IsArray = type.IsArray
            };

            if (type.GetInterface("System.Collections.IList") != null)
                data.IsList = true;

            foreach (PropertyInfo p_info in type.GetProperties()) {
                if (p_info.Name != "Item")
                    continue;

                ParameterInfo[] parameters = p_info.GetIndexParameters();

                if (parameters.Length != 1)
                    continue;

                if (parameters[0].ParameterType == typeof(int))
                    data.ElementType = p_info.PropertyType;
            }

            lock (array_metadata_lock) {
                try {
                    array_metadata.Add(type, data);
                } catch (ArgumentException) {
                    return;
                }
            }
        }

        private static void AddObjectMetadata(Type type)
        {
            if (object_metadata.ContainsKey(type))
                return;

            ObjectMetadata data = new ObjectMetadata();

            if (type.GetInterface("System.Collections.IDictionary") != null)
                data.IsDictionary = true;

            data.Properties = new Dictionary<string, PropertyMetadata>();

            foreach (PropertyInfo p_info in type.GetProperties()) {
                if (p_info.Name == "Item") {
                    ParameterInfo[] parameters = p_info.GetIndexParameters();

                    if (parameters.Length != 1)
                        continue;

                    if (parameters[0].ParameterType == typeof(string))
                        data.ElementType = p_info.PropertyType;

                    continue;
                }

                PropertyMetadata p_data = new PropertyMetadata();
                p_data.Info = p_info;
                p_data.Type = p_info.PropertyType;

                data.Properties.Add(p_info.Name, p_data);
            }

            foreach (FieldInfo f_info in type.GetFields()) {
                PropertyMetadata p_data = new PropertyMetadata {
                    Info = f_info,
                    IsField = true,
                    Type = f_info.FieldType
                };

                data.Properties.Add(f_info.Name, p_data);
            }

            lock (object_metadata_lock) {
                try {
                    object_metadata.Add(type, data);
                } catch (ArgumentException) {
                    return;
                }
            }
        }

        private static MethodInfo GetConvOp(Type t1, Type t2)
        {
            lock (conv_ops_lock) {
                if (!conv_ops.ContainsKey(t1))
                    conv_ops.Add(t1, new Dictionary<Type, MethodInfo>());
            }

            if (conv_ops[t1].ContainsKey(t2))
                return conv_ops[t1][t2];

            MethodInfo op = t1.GetMethod(
                "op_Implicit", new Type[] { t2 });

            lock (conv_ops_lock) {
                try {
                    conv_ops[t1].Add(t2, op);
                } catch (ArgumentException) {
                    return conv_ops[t1][t2];
                }
            }

            return op;
        }

        private static object ReadValue(Type inst_type, JsonReader reader)
        {
            reader.Read();

            if (reader.Token == JsonToken.ArrayEnd)
                return null;

            Type underlying_type = Nullable.GetUnderlyingType(inst_type);
            Type value_type = underlying_type ?? inst_type;

            if (reader.Token == JsonToken.Null) {

                if (inst_type.IsClass || underlying_type != null) {
                    return null;
                }

                throw new JsonException(String.Format(
                            "Can't assign null to an instance of type {0}",
                            inst_type));
            }

            if (reader.Token == JsonToken.Double ||
                reader.Token == JsonToken.Int ||
                reader.Token == JsonToken.Long ||
                reader.Token == JsonToken.String ||
                reader.Token == JsonToken.Boolean) {

                Type json_type = reader.Value.GetType();

                if (value_type.IsAssignableFrom(json_type))
                    return reader.Value;

                // Maybe there's a base importer that works
                if (base_importers_table.ContainsKey(json_type) &&
                    base_importers_table[json_type].ContainsKey(
                        value_type)) {

                    ImporterFunc importer =
                        base_importers_table[json_type][value_type];

                    return importer(reader.Value);
                }

                // Maybe it's an enum

                if (value_type.IsEnum)
                    return Enum.ToObject(value_type, reader.Value);
                // Try using an implicit conversion operator
                MethodInfo conv_op = GetConvOp(value_type, json_type);

                if (conv_op != null)
                    return conv_op.Invoke(null,
                                           new object[] { reader.Value });

                // No luck
                throw new JsonException(String.Format(
                        "Can't assign value '{0}' (type {1}) to type {2}",
                        reader.Value, json_type, inst_type));
            }

            object instance = null;

            if (reader.Token == JsonToken.ArrayStart) {

                AddArrayMetadata(inst_type);
                ArrayMetadata t_data = array_metadata[inst_type];

                if (!t_data.IsArray && !t_data.IsList)
                    throw new JsonException(String.Format(
                            "Type {0} can't act as an array",
                            inst_type));

                IList list;
                Type elem_type;

                if (!t_data.IsArray) {
                    list = (IList)Activator.CreateInstance(inst_type);
                    elem_type = t_data.ElementType;
                } else {
                    list = new ArrayList();
                    elem_type = inst_type.GetElementType();
                }

                while (true) {
                    object item = ReadValue(elem_type, reader);
                    if (item == null && reader.Token == JsonToken.ArrayEnd)
                        break;

                    list.Add(item);
                }

                if (t_data.IsArray) {
                    int n = list.Count;
                    instance = Array.CreateInstance(elem_type, n);

                    for (int i = 0; i < n; i++)
                        ((Array)instance).SetValue(list[i], i);
                } else
                    instance = list;

            } else if (reader.Token == JsonToken.ObjectStart) {
                AddObjectMetadata(value_type);
                ObjectMetadata t_data = object_metadata[value_type];

                instance = Activator.CreateInstance(value_type);

                while (true) {
                    reader.Read();

                    if (reader.Token == JsonToken.ObjectEnd)
                        break;

                    string property = (string)reader.Value;

                    if (t_data.Properties.ContainsKey(property)) {
                        PropertyMetadata prop_data =
                            t_data.Properties[property];

                        if (prop_data.IsField) {
                            ((FieldInfo)prop_data.Info).SetValue(
                                instance, ReadValue(prop_data.Type, reader));
                        } else {
                            PropertyInfo p_info =
                                (PropertyInfo)prop_data.Info;

                            if (p_info.CanWrite)
                                p_info.SetValue(
                                    instance,
                                    ReadValue(prop_data.Type, reader),
                                    null);
                            else
                                ReadValue(prop_data.Type, reader);
                        }

                    } else {
                        if (!t_data.IsDictionary) {

                            if (!reader.SkipNonMembers) {
                                throw new JsonException(String.Format(
                                        "The type {0} doesn't have the " +
                                        "property '{1}'",
                                        inst_type, property));
                            } else {
                                ReadSkip(reader);
                                continue;
                            }
                        }

                        ((IDictionary)instance).Add(
                            property, ReadValue(
                                t_data.ElementType, reader));
                    }

                }

            }

            return instance;
        }

        private static IJsonWrapper ReadValue(WrapperFactory factory,
                                               JsonReader reader)
        {
            reader.Read();

            if (reader.Token == JsonToken.ArrayEnd ||
                reader.Token == JsonToken.Null)
                return null;

            IJsonWrapper instance = factory();

            if (reader.Token == JsonToken.String) {
                instance.SetString((string)reader.Value);
                return instance;
            }

            if (reader.Token == JsonToken.Double) {
                instance.SetDouble((double)reader.Value);
                return instance;
            }

            if (reader.Token == JsonToken.Int) {
                instance.SetInt((int)reader.Value);
                return instance;
            }

            if (reader.Token == JsonToken.Long) {
                instance.SetLong((long)reader.Value);
                return instance;
            }

            if (reader.Token == JsonToken.Boolean) {
                instance.SetBoolean((bool)reader.Value);
                return instance;
            }

            if (reader.Token == JsonToken.ArrayStart) {
                instance.SetJsonType(JsonType.Array);

                while (true) {
                    IJsonWrapper item = ReadValue(factory, reader);
                    if (item == null && reader.Token == JsonToken.ArrayEnd)
                        break;

                    ((IList)instance).Add(item);
                }
            } else if (reader.Token == JsonToken.ObjectStart) {
                instance.SetJsonType(JsonType.Object);

                while (true) {
                    reader.Read();

                    if (reader.Token == JsonToken.ObjectEnd)
                        break;

                    string property = (string)reader.Value;

                    ((IDictionary)instance)[property] = ReadValue(
                        factory, reader);
                }

            }

            return instance;
        }

        private static void ReadSkip(JsonReader reader)
        {
            ToWrapper(
                delegate { return new JsonMockWrapper(); }, reader);
        }


        private static void RegisterBaseImporters()
        {
            ImporterFunc importer;

            importer = delegate (object input) {
                return Convert.ToByte((int)input);
            };
            RegisterImporter(base_importers_table, typeof(int),
                              typeof(byte), importer);

            importer = delegate (object input) {
                return Convert.ToUInt64((int)input);
            };
            RegisterImporter(base_importers_table, typeof(int),
                              typeof(ulong), importer);

            importer = delegate (object input) {
                return Convert.ToInt64((int)input);
            };
            RegisterImporter(base_importers_table, typeof(int),
                              typeof(long), importer);

            importer = delegate (object input) {
                return Convert.ToSByte((int)input);
            };
            RegisterImporter(base_importers_table, typeof(int),
                              typeof(sbyte), importer);

            importer = delegate (object input) {
                return Convert.ToInt16((int)input);
            };
            RegisterImporter(base_importers_table, typeof(int),
                              typeof(short), importer);

            importer = delegate (object input) {
                return Convert.ToUInt16((int)input);
            };
            RegisterImporter(base_importers_table, typeof(int),
                              typeof(ushort), importer);

            importer = delegate (object input) {
                return Convert.ToUInt32((int)input);
            };
            RegisterImporter(base_importers_table, typeof(int),
                              typeof(uint), importer);

            importer = delegate (object input) {
                return Convert.ToSingle((int)input);
            };
            RegisterImporter(base_importers_table, typeof(int),
                              typeof(float), importer);

            importer = delegate (object input) {
                return Convert.ToDouble((int)input);
            };
            RegisterImporter(base_importers_table, typeof(int),
                              typeof(double), importer);

            importer = delegate (object input) {
                return Convert.ToDecimal((double)input);
            };
            RegisterImporter(base_importers_table, typeof(double),
                              typeof(decimal), importer);


            importer = delegate (object input) {
                return Convert.ToUInt32((long)input);
            };
            RegisterImporter(base_importers_table, typeof(long),
                              typeof(uint), importer);

            importer = delegate (object input) {
                return Convert.ToChar((string)input);
            };
            RegisterImporter(base_importers_table, typeof(string),
                              typeof(char), importer);

            importer = delegate (object input) {
                return Convert.ToDateTime((string)input, datetime_format);
            };
            RegisterImporter(base_importers_table, typeof(string),
                              typeof(DateTime), importer);

            importer = delegate (object input) {
                return DateTimeOffset.Parse((string)input, datetime_format);
            };
            RegisterImporter(base_importers_table, typeof(string),
                typeof(DateTimeOffset), importer);
        }

        private static void RegisterImporter(
            IDictionary<Type, IDictionary<Type, ImporterFunc>> table,
            Type json_type, Type value_type, ImporterFunc importer)
        {
            if (!table.ContainsKey(json_type))
                table.Add(json_type, new Dictionary<Type, ImporterFunc>());

            table[json_type][value_type] = importer;
        }

        #endregion



        public static JsonData ToObject(string json)
        {
            return (JsonData)ToWrapper(
                delegate { return new JsonData(); }, json);
        }


        public static IJsonWrapper ToWrapper(WrapperFactory factory,
                                              JsonReader reader)
        {
            return ReadValue(factory, reader);
        }

        public static IJsonWrapper ToWrapper(WrapperFactory factory,
                                              string json)
        {
            JsonReader reader = new JsonReader(json);

            return ReadValue(factory, reader);
        }



    }
}
