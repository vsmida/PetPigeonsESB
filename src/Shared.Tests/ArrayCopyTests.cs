﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Reflection;
using NUnit.Framework;

namespace Shared.Tests
{

    unsafe delegate void MemCpyImpl(byte* src, byte* dest, int len);
    [TestFixture, Ignore]
    public class ArrayCopyTests
    {

       static MemCpyImpl _memcpyimpl = (MemCpyImpl)Delegate.CreateDelegate(
typeof(MemCpyImpl), typeof(Buffer).GetMethod("Memcpy",BindingFlags.NonPublic | BindingFlags.Static, Type.DefaultBinder,
new Type[]{typeof(byte*),typeof(byte*), typeof(int)},null));
        [Test]
        public void compare_methods()
        {


            var repeatCount = 5000;
            var sizesToTest = new List<int> { 4, 10, 100, 1000, 10000, 100000 };
            foreach (var size in sizesToTest)
            {
                Test("Naive", repeatCount, size, (x, y) =>
                {
                    for (int i = 0; i < x.Length; i++)
                    {

                        y[i] = x[i];
                    }
                }, w => "us = " + w.ElapsedMicroseconds());
                Test("ArrayCOpy", repeatCount, size, (x, y) => Array.Copy(x, 0, y, 0, x.Length), w => "us = " + w.ElapsedMicroseconds());
                Test("BlockCopy", repeatCount, size, (x, y) => Buffer.BlockCopy(x, 0, y, 0, x.Length), w => "us = " + w.ElapsedMicroseconds());
                Test("MemCopy", repeatCount, size, (x, y) =>
                                                       {
                                                           unsafe
                                                           {
                                                               fixed (byte* source = x, destination = y)
                                                               {
                                                                   _memcpyimpl(source, destination, x.Length);
                                                               }
                                                           }
                                                       }, w => "us = " + w.ElapsedMicroseconds());
                Test("UnsafeIterationCopy", repeatCount, size, (x, y) =>
                                                         {
                                                             unsafe
                                                             {
                                                                 fixed (byte* source = x, destination = y)
                                                                 {
                                                                     for (int i = 0; i < x.Length; i++)
                                                                     {
                                                                         destination[i] = source[i];
                                                                     }
                                                                 }
                                                             }


                                                         }, w => "us = " + w.ElapsedMicroseconds());


            }



        }
        private void Test(string methodName, int repeatCount, int size, Action<byte[], byte[]> code, Func<Stopwatch, string> timeoutput)
        {
            var sourceArray = new byte[size];
            var destinationArray = new byte[size];
            var watch = new Stopwatch();
            watch.Start();
            for (int repeat = 0; repeat < repeatCount; repeat++)
            {
                code(sourceArray, destinationArray);
            }
            watch.Stop();
            Console.WriteLine(string.Format("Size = {0}, Method = {1}, " + timeoutput(watch), size, methodName));
        }

    }
}