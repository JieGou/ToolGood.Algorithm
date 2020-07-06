﻿using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using static mathParser;

namespace ToolGood.Algorithm
{
    public class AlgorithmEngine
    {
        /// <summary>
        /// 使用EXCEL索引
        /// </summary>
        public bool UseExcelIndex { get; set; } = true;
        /// <summary>
        /// 最后一个错误
        /// </summary>
        public string LastError { get; private set; }
        private ProgContext _context;
        private Dictionary<string, Operand> _dict = new Dictionary<string, Operand>();

        #region GetParameter
        protected virtual Operand GetParameter(string parameter)
        {
            if (_dict.TryGetValue(parameter, out Operand operand))
            {
                return operand;
            }
            return Operand.Error($"Parameter [{parameter}] is missing.");
        }
        #endregion

        #region AddParameter
        public void AddParameter(string key, Operand obj)
        {
            _dict[key] = obj;
        }
        public void AddParameter(string key, bool obj)
        {
            _dict[key] = Operand.Create(obj);
        }
        public void AddParameter(string key, int obj)
        {
            _dict[key] = Operand.Create(obj);
        }
        public void AddParameter(string key, double obj)
        {
            _dict[key] = Operand.Create(obj);
        }
        public void AddParameter(string key, string obj)
        {
            _dict[key] = Operand.Create(obj);
        }
        public void AddParameter(string key, Date obj)
        {
            _dict[key] = Operand.Create(obj);
        }
        public void AddParameter(string key, DateTime obj)
        {
            _dict[key] = Operand.Create(obj);
        }
        public void AddParameter(string key, TimeSpan obj)
        {
            _dict[key] = Operand.Create(obj);
        }

        public void AddParameter(string key, List<Operand> obj)
        {
            _dict[key] = Operand.Create(obj);
        }
        public void AddParameter(string key, ICollection<string> obj)
        {
            _dict[key] = Operand.Create(obj);
        }
        public void AddParameter(string key, ICollection<double> obj)
        {
            _dict[key] = Operand.Create(obj);

        }
        public void AddParameter(string key, ICollection<int> obj)
        {
            _dict[key] = Operand.Create(obj);

        }
        public void AddParameter(string key, ICollection<bool> obj)
        {
            _dict[key] = Operand.Create(obj);
        }
        #endregion

        #region Parse
        class AntlrErrorListener : IAntlrErrorListener<IToken>
        {
            public bool IsError { get; private set; }
            public string ErrorMsg { get; private set; }

            public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                IsError = true;
                ErrorMsg = msg;
            }
        }

        /// <summary>
        /// 编译公式，默认
        /// </summary>
        /// <param name="exp">公式</param>
        /// <returns></returns>
        public bool Parse(string exp)
        {
            if (string.IsNullOrWhiteSpace(exp))
            {
                LastError = "Parameter exp invalid !";
                return false;
            }
            //try {

            var stream = new CaseChangingCharStream(new AntlrInputStream(exp));
            var lexer = new mathLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new mathParser(tokens);
            var antlrErrorListener = new AntlrErrorListener();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(antlrErrorListener);

            var context = parser.prog();
            var end = context.Stop.StopIndex;
            if (end + 1 < exp.Length)
            {
                _context = null;
                LastError = "Parameter exp invalid !";
                return false;
            }
            if (antlrErrorListener.IsError)
            {
                _context = null;
                LastError = antlrErrorListener.ErrorMsg;
                return false;
            }
            _context = context;
            return true;
            //} catch (Exception ex) {
            //    LastError = ex.Message;
            //    return false;
            //}
        }
        #endregion

        #region Evaluate
        /// <summary>
        /// 执行函数
        /// </summary>
        /// <returns></returns>
        public Operand Evaluate()
        {
            if (_context == null)
            {
                LastError = "Please use Parse to compile formula !";
                throw new Exception("Please use Parse to compile formula !");
            }
            var visitor = new MathVisitor();
            visitor.GetParameter += GetParameter;
            visitor.excelIndex = UseExcelIndex ? 1 : 0;
            return visitor.Visit(_context);
        } 
        #endregion

        #region TryEvaluate

        public int TryEvaluate(string exp, int def)
        {
            if (Parse(exp))
            {
                try
                {
                    var obj = Evaluate();
                    obj = obj.ToNumber("It can't be converted to number!");
                    if (obj.IsError)
                    {
                        LastError = obj.ErrorMsg;
                        return def;
                    }
                    return obj.IntValue;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                }
            }
            return def;
        }

        public double TryEvaluate(string exp, double def)
        {
            if (Parse(exp))
            {
                try
                {
                    var obj = Evaluate();
                    obj = obj.ToNumber("It can't be converted to number!");
                    if (obj.IsError)
                    {
                        LastError = obj.ErrorMsg;
                        return def;
                    }
                    return obj.NumberValue;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                }
            }
            return def;
        }

        public string TryEvaluate(string exp, string def)
        {
            if (Parse(exp))
            {
                try
                {
                    var obj = Evaluate();
                    if (obj.IsNull) {
                        return null;
                    }
                    obj = obj.ToString("It can't be converted to string!");
                    if (obj.IsError)
                    {
                        LastError = obj.ErrorMsg;
                        return def;
                    }
                    return obj.StringValue;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                }
            }
            return def;
        }
        public bool TryEvaluate(string exp, bool def)
        {
            if (Parse(exp))
            {
                try
                {
                    var obj = Evaluate();
                    obj = obj.ToBoolean("It can't be converted to bool!");
                    if (obj.IsError)
                    {
                        LastError = obj.ErrorMsg;
                        return def;
                    }
                    return obj.BooleanValue;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                }
            }
            return def;
        }

        public DateTime TryEvaluate(string exp, DateTime def)
        {
            if (Parse(exp))
            {
                try
                {
                    var obj = Evaluate();
                    obj = obj.ToDate("It can't be converted to date!");
                    if (obj.IsError)
                    {
                        LastError = obj.ErrorMsg;
                        return def;
                    }
                    return (DateTime) obj.DateValue;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                }
            }
            return def;
        }

        public TimeSpan TryEvaluate(string exp, TimeSpan def)
        {
            if (Parse(exp))
            {
                try
                {
                    var obj = Evaluate();
                    obj = obj.ToDate("It can't be converted to date!");
                    if (obj.IsError)
                    {
                        LastError = obj.ErrorMsg;
                        return def;
                    }
                    return (TimeSpan) obj.DateValue;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                }
            }
            return def;
        }
        #endregion
    }
}
