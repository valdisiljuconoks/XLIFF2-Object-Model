using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Localization.Xliff.OM.Core;
using Localization.Xliff.OM.Serialization;
using Xunit;
using File = Localization.Xliff.OM.Core.File;

namespace Xliff.UnitTests
{
    public class EncoderTests
    {
        public EncoderTests()
        {
            var settings = new XliffWriterSettings
                           {
                               Indent = true,
                               IndentChars = "    "
                           };

            settings.Validators.Clear();

            _document = new XliffDocument("en-GB")
                        {
                            TargetLanguage = "en-US"
                        };

            _stream = new MemoryStream();
            _writer = new XliffWriter(settings);
        }

        private MemoryStream _stream;
        private XliffDocument _document;
        private XliffWriter _writer;

        private string Serialize()
        {
            _stream.SetLength(0);
            _stream.Seek(0, SeekOrigin.Begin);
            _writer.Serialize(_stream, _document);

            return GetStreamContents(_stream);
        }

        private static string GetStreamContents(Stream stream)
        {
            string result;

            stream.Seek(0, SeekOrigin.Begin);
            using (TextReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
            {
                result = reader.ReadToEnd();
            }

            return result;
        }

        private static Stream AsStream(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;

            return stream;
        }

        [Fact]
        public void XliffWriter_InvalidSegmentId()
        {
            var unit = new Unit("u1");
            var segment = new Segment(XmlConvert.EncodeNmToken("/this/is/legacy/id"))
                          {
                              Source = new Source(),
                              Target = new Target()
                          };

            segment.Source.Text.Add(new CDataTag("from-source"));
            segment.Target.Text.Add(new CDataTag("to-target"));

            unit.Resources.Add(segment);

            var segment2 = new Segment(XmlConvert.EncodeNmToken("this+is+nested"))
                          {
                              Source = new Source(),
                              Target = new Target()
                          };

            segment2.Source.Text.Add(new CDataTag("from-source-nested"));
            segment2.Target.Text.Add(new CDataTag("to-target-nested"));

            unit.Resources.Add(segment2);

            var segment3 = new Segment(XmlConvert.EncodeNmToken("this.is.normal.id"))
                          {
                              Source = new Source(),
                              Target = new Target()
                          };

            segment3.Source.Text.Add(new CDataTag("from-source-normal"));
            segment3.Target.Text.Add(new CDataTag("to-target-normal"));

            unit.Resources.Add(segment3);

            _document.Files.Add(new File("f1"));
            _document.Files[0].Containers.Add(unit);

            var actualValue = Serialize();

            var reader = new XliffReader();
            var doc = reader.Deserialize(AsStream(actualValue));

            var resources = doc.Files[0].Containers.OfType<Unit>().First().Resources;

            Assert.NotNull(resources.FirstOrDefault(r => XmlConvert.DecodeName(r.Id) == "this+is+nested"));
            Assert.NotNull(resources.FirstOrDefault(r => XmlConvert.DecodeName(r.Id) == "/this/is/legacy/id"));
            Assert.NotNull(resources.FirstOrDefault(r => XmlConvert.DecodeName(r.Id) == "this.is.normal.id"));
        }
    }
}
