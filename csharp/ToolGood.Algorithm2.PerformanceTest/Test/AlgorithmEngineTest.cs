﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ToolGood.Algorithm;

namespace ToolGood.Algorithm2.PerformanceTest.Test
{
    public class AlgorithmEngineTest
    {
        [Benchmark]
        public void Test_Add()
        {
            AlgorithmEngine engine = new AlgorithmEngine();
            var c = engine.TryEvaluate("2+3", 0);
        }

        [Benchmark]
        public void Test_Add2()
        {
            AlgorithmEngine engine = new AlgorithmEngine();
            var s = engine.TryEvaluate("'aa'&'bb'", "");
        }

        [Benchmark]
        public void Test_Add_3()
        {
            AlgorithmEngine engine = new AlgorithmEngine();
            var c = engine.TryEvaluate("2+true", 0);
        }

        [Benchmark]
        public void Test_Add_4()
        {
            AlgorithmEngine engine = new AlgorithmEngine();
            var c = engine.TryEvaluate("2+'12:0'", 0);
        }

        [Benchmark]
        public void Test_Sub()
        {
            AlgorithmEngine engine = new AlgorithmEngine();
            var c = engine.TryEvaluate("2-3", 0);
        }


        [Benchmark]
        public void Test_Mul()
        {
            AlgorithmEngine engine = new AlgorithmEngine();
            var c = engine.TryEvaluate("2*3", 0);
        }

        [Benchmark]
        public void Test_Mul_2()
        {
            AlgorithmEngine engine = new AlgorithmEngine();
            var c = engine.TryEvaluate("'1:0'*3", 0);
        }

        [Benchmark]
        public void Test_Div()
        {
            AlgorithmEngine engine = new AlgorithmEngine();
            var c = engine.TryEvaluate("2/3", 0);
        }

        [Benchmark]
        public void Test_Div_2()
        {
            AlgorithmEngine engine = new AlgorithmEngine();
            var c = engine.TryEvaluate("'9:0'/3", 0);
        }


        [Benchmark]
        public void Test_c()
        {
            AlgorithmEngine engine = new AlgorithmEngine();
            var c = engine.TryEvaluate("2>3", false);
        }

        [Benchmark]
        public void Test_c2()
        {
            AlgorithmEngine engine = new AlgorithmEngine();
            var c = engine.TryEvaluate("2>=3", false);
        }

        [Benchmark]
        public void Test_c3()
        {
            AlgorithmEngine engine = new AlgorithmEngine();
            var c = engine.TryEvaluate("2<3", false);
        }

        [Benchmark]
        public void Test_c4()
        {
            AlgorithmEngine engine = new AlgorithmEngine();
            var c = engine.TryEvaluate("2<=3", false);
        }

        [Benchmark]
        public void Test_c5()
        {
            AlgorithmEngine engine = new AlgorithmEngine();
            var c = engine.TryEvaluate("2==3", false);
        }

    



    }
}
