using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using NUnit.Framework;
using ServiceStack.Text;
using Should;

namespace WebApiContrib.Formatting.ServiceStack.Tests
{
    public class CustomType
    {
        public int Number { get; set; }
        public string Text { get; set; }
    }

    public class CustomTypeContainer
    {
        public CustomType Member1 { get; set; }
        public CustomType Member2 { get; set; }
    }

    public class CustomTypeSerialization
    {
        public string Serialize(CustomType value)
        {
            return string.Format("{0} - {1}", value.Number, value.Text);
        }

        public CustomType Deserialize(string text)
        {
            var tokens = text.Split('-');
            var value = new CustomType {Number = int.Parse(tokens.First().Trim()), Text = tokens.Last().Trim()};

            return value;
        }
    }

    [TestFixture]
    public class CustomSerializerFormattingTests
    { 
        [Test]
        public void Should_round_trip_with_custom_serialisation()
        {
            // setup custom serialisation
            var serializer = new CustomTypeSerialization();
            JsConfig<CustomType>.SerializeFn = serializer.Serialize;
            JsConfig<CustomType>.DeSerializeFn = serializer.Deserialize;

            var formatter = new ServiceStackTextFormatter();
            var value = new CustomTypeContainer()
            {
                Member1 = new CustomType(){ Number = 2, Text = "lorem ipsum"},
                Member2 = new CustomType(){ Number = 3, Text = "dolor sit amet"}
            };

            // serialisation 1
            var dehydrated = Serialise(formatter, value);

            // deserialise from dehydrated string
            var rehydrated = Deserialise<CustomTypeContainer>(formatter, dehydrated);

            rehydrated.Member1.ShouldNotBeNull();
            rehydrated.Member2.ShouldNotBeNull();
            rehydrated.Member1.Number.ShouldEqual(value.Member1.Number);
            rehydrated.Member1.Text.ShouldEqual(value.Member1.Text);
            rehydrated.Member2.Number.ShouldEqual(value.Member2.Number);
            rehydrated.Member2.Text.ShouldEqual(value.Member2.Text);
        }

        private T Deserialise<T>(ServiceStackTextFormatter formatter, string dehydrated)
        {
            var utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            var stream = new MemoryStream(utf8Encoding.GetBytes(dehydrated));
            var content = new StringContent(string.Empty);

            var deserialisationResultTask = formatter.ReadFromStreamAsync(typeof(T), stream, content, null);
            deserialisationResultTask.Wait();

            deserialisationResultTask.Result.ShouldBeType<T>();

            var rehydrated = (T)deserialisationResultTask.Result;
            return rehydrated;
        }

        internal string Serialise<T>(ServiceStackTextFormatter formatter, T value)
        {
            var content = new StringContent(string.Empty);
            var stream = new MemoryStream();

            var serialisationResultTask = formatter.WriteToStreamAsync(typeof(T), value, stream, content, transportContext: null);
            serialisationResultTask.Wait();

            stream.Position = 0;
            string dehydrated = new StreamReader(stream).ReadToEnd();

            return dehydrated;
        }
    }
}
