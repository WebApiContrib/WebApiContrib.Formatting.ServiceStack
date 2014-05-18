using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using NUnit.Framework;
using NodaTime;
using NodaTime.Serialization.ServiceStackText;
using Should;

namespace WebApiContrib.Formatting.ServiceStack.Tests
{
    [TestFixture]
    class NodaTimeFormattingTests
    { 
        [Test]
        public void Should_round_trip_with_custom_serialisation()
        {
            // set up the noda time formatting defaults
            DateTimeZoneProviders.Tzdb
                                 .CreateDefaultSerializersForNodaTime()
                                 .ConfigureSerializersForNodaTime();

            var formatter = new ServiceStackTextFormatter();
            var value = new LocalDateTimeContainer()
                {
                    DateTime = new LocalDateTime(2014, 05, 18, 09, 00),
                    OffsetDateTime = new OffsetDateTime(new LocalDateTime(2014, 05, 18, 17, 00), Offset.FromHours(10))
                };

            // serialisation 1
            var dehydrated = Serialise(formatter, value);

            // deserialise from dehydrated string
            var rehydrated = Deserialise<LocalDateTimeContainer>(formatter, dehydrated);

            rehydrated.DateTime.ShouldEqual(value.DateTime);
            rehydrated.OffsetDateTime.ShouldEqual(value.OffsetDateTime);
        }

        private T Deserialise<T>(ServiceStackTextFormatter formatter, string dehydrated)
        {
            var utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            var stream = new MemoryStream(utf8Encoding.GetBytes(dehydrated));
            var content = new StringContent(string.Empty);

            var deserialisationResultTask = formatter.ReadFromStreamAsync(typeof(LocalDateTimeContainer), stream, content, null);
            deserialisationResultTask.Wait();

            deserialisationResultTask.Result.ShouldBeType<LocalDateTimeContainer>();

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

    public class LocalDateTimeContainer
    {
        public LocalDateTime DateTime { get; set; }
        public OffsetDateTime OffsetDateTime { get; set; }
    }
}
