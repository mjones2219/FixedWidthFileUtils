﻿using FixedWidthFileUtils;
using FixedWidthFileUtils.Serializers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestDeserializeWFFile()
        {
            PositivePayFile actualFile = FixedWidthSerializer.Deserialize<PositivePayFile>(Properties.Resources.pos_AP_20200207211501810);

            PositivePayFile expectedFile = new PositivePayFile
            {
                Header = new FileHeader
                {
                    AccountNumber = 5555333111,
                    BankID = 164
                },
                CheckGroups = new[]
                {
                    new CheckGroup
                    {
                        Records = new []
                        {
                            new CheckRecord
                            {
                                CheckSerial = 19983,
                                AccountNumber = 5555333111,
                                Amount = 1050.00m,
                                IssueDate = new DateTime(2020, 02, 03),
                                Payee = "T & T TESTPAYEE, L.P"
                            }
                        },
                        Trailer = new CheckGroupTrailer
                        {
                            RecordCount = 1,
                            TotalAmount = 1050.00m
                        }
                    }
                }
            };


            Assert.IsTrue(expectedFile.ObjectEquals(actualFile));
        }
    }

    public static class TestExtensions
    {
        public static bool ObjectEquals<T>(this T source, T target)
        {
            if (source == null && target == null) return true;
            if (source != null && target == null) return false;
            if (source == null && target != null) return false;

            var props = source.GetType().GetProperties().Where(p => p.CanRead);
            foreach (var prop in props)
            {
                if (prop.PropertyType.IsEnumerable())
                {
                    var leftEnumerable = prop.GetValue(source) as ICollection;
                    var rightEnumerable = prop.GetValue(target) as ICollection;

                    if (leftEnumerable == null && rightEnumerable == null) continue;
                    if (leftEnumerable == null && rightEnumerable != null) return false;
                    if (leftEnumerable != null & rightEnumerable == null) return false;
                    if (leftEnumerable.Count != rightEnumerable.Count) return false;

                    IEnumerator leftEnumerator = leftEnumerable.GetEnumerator();
                    IEnumerator rightEnumerator = rightEnumerable.GetEnumerator();
                    while (leftEnumerator.MoveNext() && rightEnumerator.MoveNext())
                    {
                        if (!ObjectEquals(leftEnumerator.Current, rightEnumerator.Current))
                            return false;
                    }
                }
                else if (prop.PropertyType.IsComplexType())
                {
                    var left = prop.GetValue(source);
                    var right = prop.GetValue(target);

                    if (left == null && right == null) continue;
                    if ((left == null && right != null) || (left != null && right == null))
                        return false;

                    if (!ObjectEquals(left, right)) return false;
                } 
                else
                {
                    var defaultComparerProperty = typeof(EqualityComparer<>)
                        .MakeGenericType(prop.PropertyType)
                        .GetProperty("Default")
                        .GetValue(null, null);

                    var methodInfo = defaultComparerProperty.GetType().GetMethod("Equals", new Type[] { prop.PropertyType, prop.PropertyType });
                    bool result = (bool)methodInfo.Invoke(defaultComparerProperty, new[] { prop.GetValue(source), prop.GetValue(target) });
                    if (!result) return false;
                }
            }
            return true;
        }
        public static bool IsComplexType(this Type t)
        {
            return !t.IsValueType && t != typeof(string);
        }
        public static bool IsEnumerable(this Type t)
        {
            return typeof(IEnumerable).IsAssignableFrom(t) && t != typeof(string);
        }
    }
    
    #region MODELS
    public class PositivePayFile
    {
        [FixedField(0)]
        public FileHeader Header { get; set; }

        [FixedField(1)]
        public CheckGroup[] CheckGroups { get; set; }
    }
    public class FileHeader
    {
        [FixedField(0, 3)]
        public string Start => "*03";

        [FixedField(1, 5)]
        public int BankID { get; set; }

        [FixedField(2, 15)]
        public long AccountNumber { get; set; }

        [FixedField(3, 1)]
        public int AlwaysZero => 0;
    }
    public class CheckRecord
    {
        [FixedField(0, 10)]
        public long CheckSerial { get; set; }

        [FixedField(1, 6)]
        [FixedFieldSerializer(typeof(WellsFargoDateSerializer))]
        public DateTime IssueDate { get; set; }

        [FixedField(2, 15)]
        public long AccountNumber { get; set; }

        [FixedField(3, 3)]
        public int TransactionCode => 320;

        [FixedField(4, 10)]
        [FixedFieldSerializer(typeof(DecimalToPenniesSerializer))]
        public decimal Amount { get; set; }

        private string _Payee;
        [FixedField(5, 41, ' ', FixedFieldAlignment.Left)]
        public string Payee
        {
            get => _Payee?.Trim();
            set => _Payee = value;
        }
    }
    public class CheckGroup
    {
        [FixedField(0)]
        public CheckRecord[] Records { get; set; }

        [FixedField(1)]
        public CheckGroupTrailer Trailer { get; set; }
    }
    public class CheckGroupTrailer
    {
        [FixedField(0, 15, ' ', FixedFieldAlignment.Left)]
        public string Start => "&";

        [FixedField(1, 5)]
        public int RecordCount { get; set; }

        [FixedField(3, 10)]
        [FixedFieldSerializer(typeof(DecimalToPenniesSerializer))]
        public decimal TotalAmount { get; set; }

        [FixedField(2, 3, ' ')]
        [FixedField(4, 47, ' ')]
        public string Spacer => string.Empty;
    }
    #endregion

    #region CUSTOM SERIALIZER
    public class WellsFargoDateSerializer : FixedFieldSerializer<DateTime>
    {
        public override DateTime Deserialize(string input) => DateTime.ParseExact(input, "MMddyy", CultureInfo.InvariantCulture);
        public override string Serialize(DateTime input) => input.ToString("MMddyy");
    }
    #endregion
}
