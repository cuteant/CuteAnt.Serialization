using System;
using System.Collections.Generic;
using CuteAnt;
using Newtonsoft.Json;
#if !NET40
using SpanJson;
#endif
#if DNXCORE50
using Xunit;
using Test = Xunit.FactAttribute;
using Assert = JsonExtensions.Tests.XUnitAssert;
#else
using NUnit.Framework;
#endif

namespace JsonExtensions.Tests
{
    public class JsonObjectTypeDeserializerTest
    {
        [Test]
        public void Run()
        {
            var dict = new Dictionary<string, object>
            {
                { "KeyA", 101 },
                { "KeyB", Guid.NewGuid() },
                { "KeyC", CombGuid.NewComb() },
            };

            var json = JsonConvertX.SerializeObject(dict);
            var newDict = JsonConvertX.DeserializeObject<Dictionary<string, object>>(json);

            Assert.AreEqual((int)dict["KeyA"], newDict.Deserialize<int>("KeyA"));
            Assert.AreEqual((Guid)dict["KeyB"], newDict.Deserialize<Guid>("KeyB"));
            Assert.AreEqual((CombGuid)dict["KeyC"], newDict.Deserialize<CombGuid>("KeyC"));

#if !NET40
            json = SpanJson.JsonSerializer.Generic.Utf16.Serialize(dict);
            newDict = SpanJson.JsonSerializer.Generic.Utf16.Deserialize<Dictionary<string, object>>(json);

            Assert.AreEqual((int)dict["KeyA"], newDict.Deserialize<int>("KeyA"));
            Assert.AreEqual((Guid)dict["KeyB"], newDict.Deserialize<Guid>("KeyB"));
            Assert.AreEqual((CombGuid)dict["KeyC"], newDict.Deserialize<CombGuid>("KeyC"));
#endif
        }
    }
}
