using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.HtmlZipOld
{

    /// <summary>
    /// HTMLParser is an object that can decode mhtml into ASCII text.
    /// Using getHTMLText() will generate static HTML with inline images. 
    /// 
    /// Originally based on this project https://github.com/DavidBenko/MHTML-to-HTML-Decoding-in-C-Sharp
    /// </summary>
    /// <summary>
    /// HTMLParser is an object that can decode mhtml into ASCII text.
    /// Using getHTMLText() will generate static HTML with inline images. 
    /// </summary>
    public class MHTMLParser
    {
        const string BOUNDARY = "boundary";
        const string CHAR_SET = "charset";
        const string CONTENT_TYPE = "Content-Type";
        const string CONTENT_TRANSFER_ENCODING = "Content-Transfer-Encoding";
        const string CONTENT_LOCATION = "Content-Location";
        const string FILE_NAME = "filename=";

        private Dictionary<String, String> imageMap = new Dictionary<string, string>();
        private Dictionary<String, String> cssMap = new Dictionary<string, string>();

        private string mhtmlString; // the string we want to decode
        private string log; // log file
        public bool DecodeImageData { get; set; }

        /// <summary>
        /// Directory were to save images and css and all parts that needs to 
        /// be saved
        /// </summary>
        public String OutputDirectory { get; set; }
        private List<PartInfo> dataset;

        /*
         * Default Constructor
         */
        public MHTMLParser()
        {
            this.dataset = new List<PartInfo>(); //Init dataset
            this.log += "Initialized dataset.\n";
            this.DecodeImageData = false; //Set default for decoding images
        }

        /*
         * Init with contents of string 
         */
        public MHTMLParser(string mhtml)
            : this()
        {
            setMHTMLString(mhtml);
        }
        /*
         * Init with contents of string, and decoding option
         */
        public MHTMLParser(string mhtml, bool decodeImages)
            : this(mhtml)
        {
            this.DecodeImageData = decodeImages;
        }
        /*
         * Set the mhtml string we want to decode
         */
        public void setMHTMLString(string mhtml)
        {
            try
            {
                if (mhtml == null) throw new Exception("The mhtml string is null"); //Early Exit
                this.mhtmlString = mhtml; //Set String
                this.log += "Set mhtml string.\n";
            }
            catch (Exception e)
            {
                this.log += e.Message;
                this.log += e.StackTrace;
            }
        }
        /*
         * Decompress Archive From String
         */
        private List<PartInfo> decompressString()
        {
            // init Prerequisites
            StringReader reader = null;
            string type = "";
            string encoding = "";
            string location = "";
            string filename = "";
            string charset = "utf-8";
            StringBuilder buffer = null;
            this.log += "Starting decompression \n";


            try
            {
                reader = new StringReader(this.mhtmlString); //Start reading the string

                String boundary = getBoundary(reader); // Get the boundary code
                if (boundary == null) throw new Exception("Failed to find string 'boundary'");
                this.log += "Found boundary.\n";

                //Loop through each line in the string
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    string temp = line.Trim();
                    if (temp.Contains(boundary)) //Check if this is a new section
                    {
                        if (buffer != null) //If this is a new section and the buffer is full, write to dataset
                        {
                            var bufferContent = writeBufferContent(buffer, encoding, charset, type, this.DecodeImageData, location);
                            PartInfo info = new PartInfo()
                            {
                                ContentType = type,
                                ContentName = filename,
                                DataAsString = bufferContent,
                                Location = location,
                            };
                            if ("text/css".Equals(type, StringComparison.OrdinalIgnoreCase))
                            {
                                var fileName = ToSafeFileName(location) + ".css";
                                File.WriteAllText(fileName, info.DataAsString);
                                cssMap[location] = fileName;
                            }
                            else if (type.Contains("image"))
                            {
                                this.log += "Image Data Detected.\n";
                                if (DecodeImageData)
                                {
                                    if (encoding != "base64")
                                        throw new NotImplementedException("Cannot decode if the encoding is not base64");

                                    var b = Convert.FromBase64String(buffer.ToString());
                                    info.DataAsBinary = b;
                                    using (var ms = new MemoryStream(b))
                                    {
                                        using (var image = Image.FromStream(ms))
                                        {
                                            var fileName = ToSafeFileName(location);
                                            var realLocation = Path.Combine(this.OutputDirectory, fileName);
                                            image.Save(realLocation);
                                            imageMap[location] = realLocation;
                                        }
                                    }

                                }
                            }

                            this.dataset.Add(info);
                            buffer = null;
                            this.log += "Wrote Buffer Content and reset buffer.\n";
                        }
                        buffer = new StringBuilder();
                    }
                    else if (temp.StartsWith(CONTENT_TYPE))
                    {
                        type = getAttribute(temp);
                        this.log += "Got content type.\n";
                    }
                    else if (temp.StartsWith(CHAR_SET))
                    {
                        charset = getCharSet(temp);
                        this.log += "Got charset.\n";
                    }
                    else if (temp.StartsWith(CONTENT_TRANSFER_ENCODING))
                    {
                        encoding = getAttribute(temp);
                        this.log += "Got encoding (" + encoding + ").\n";
                    }
                    else if (temp.StartsWith(CONTENT_LOCATION))
                    {
                        location = temp.Substring(temp.IndexOf(":") + 1).Trim();
                        this.log += "Got location.\n";
                    }
                    else if (temp.StartsWith(FILE_NAME))
                    {
                        char c = '"';
                        filename = temp.Substring(temp.IndexOf(c.ToString()) + 1, temp.LastIndexOf(c.ToString()) - temp.IndexOf(c.ToString()) - 1);
                    }
                    else if (temp.StartsWith("Content-ID") || temp.StartsWith("Content-Disposition") || temp.StartsWith("name=") || temp.Length == 1)
                    {
                        //We don't need this stuff; Skip lines
                    }
                    else
                    {
                        if (buffer != null)
                        {
                            if (buffer.Length > 0 | !string.IsNullOrEmpty(line))
                            {
                                if ("quoted-printable".Equals(encoding, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (line.EndsWith("="))
                                    {
                                        buffer.Append(line.Substring(0, line.Length - 1));
                                    }
                                    else
                                    {
                                        buffer.Append(line + "\n");
                                    }
                                }
                                else
                                {
                                    buffer.Append(line + "\n");
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (null != reader)
                    reader.Close();
                this.log += "Closed Reader.\n";
            }
            return this.dataset; //Return Results
        }
        private string writeBufferContent(StringBuilder buffer, string encoding, string charset, string type, bool decodeImages, string location)
        {
            this.log += "Start writing buffer contents.\n";

            // base64 Decoding
            if (encoding.ToLower().Equals("base64"))
            {
                try
                {
                    this.log += "base64 encoding detected.\n";
                    this.log += "Got base64 decoded string.\n";
                    return decodeFromBase64(buffer.ToString());
                }
                catch (Exception e)
                {
                    this.log += e.Message + "\n";
                    this.log += e.StackTrace + "\n";
                    this.log += "Data not Decoded.\n";
                    return buffer.ToString();
                }
            }
            //quoted-printable decoding
            else if (encoding.ToLower().Equals("quoted-printable"))
            {
                this.log += "Quoted-Prinatble string detected.\n";
                return getQuotedPrintableString(buffer.ToString(), charset);
            }
            else
            {
                this.log += "Unknown Encoding.\n";
                return buffer.ToString();
            }
        }
        /*
         * Take base64 string, get bytes and convert to ascii string
         */
        static public string decodeFromBase64(string encodedData)
        {
            byte[] encodedDataAsBytes
                = System.Convert.FromBase64String(encodedData);
            string returnValue =
               System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);
            return returnValue;
        }
        /*
         * Get decoded quoted printable string
         */
        public string getQuotedPrintableString(string mimeString, string charSet)
        {
            try
            {
                //Remove Byte Order Mark for UTF8
                if (mimeString.StartsWith("=EF=BB=BF"))
                    mimeString = mimeString.Substring("=EF=BB=BF".Length);
                return DecodeQuotedPrintables(mimeString, charSet);
            }
            catch (Exception e)
            {
                this.log += e.Message + "\n";
                this.log += e.StackTrace + "\n";
                this.log += "Data not Decoded.\n";
                return mimeString;
            }
        }
        /*
         * Finds boundary used to break code into multiple parts
         */
        private string getBoundary(StringReader reader)
        {
            string line = null;
            Int32 lineCount = 0;
            while ((line = reader.ReadLine()) != null)
            {
                var match = Regex.Match(line, "boundary=\"(?<boundary>.*)\"");
                if (match.Success)
                {
                    return match.Groups["boundary"].Value;
                }

                if (lineCount++ > 50) return null;
            }
            return null;
        }
        /*
         * Grabs charset from a line 
         */
        private string getCharSet(String temp)
        {
            string t = temp.Split('=')[1].Trim(' ', '"');
            return t;
        }
        /*
         * split a line on ": "
         */
        private string getAttribute(String line)
        {
            string str = ": ";
            return line.Substring(line.IndexOf(str) + str.Length, line.Length - (line.IndexOf(str) + str.Length)).Replace(";", "");
        }
        /*
         * Get an html page from the mhtml. Embeds images as base64 data
         */
        public string getHTMLText()
        {
            // if (this.decodeImageData) throw new Exception("Turn off image decoding for valid html output.");
            var data = this.decompressString();
            string body = "";
            //First, lets write all non-images to mail body
            //Then go back and add images in 
            String bodyLocation = "";
            foreach (var part in data)
            {
                if (part.ContentType.Contains("text/html"))
                {
                    body += part.DataAsString;
                    this.log += "Writing HTML Text\n";
                    bodyLocation = part.Location;
                }
                //else if (part.ContentType.Contains("image"))
                //{
                //    body = body.Replace("cid:" + part.ContentName, "data:" + part.ContentType + ";base64," + part.DataAsString);
                //    this.log += "Overwriting HTML with image: " + part.ContentName + "\n";
                //}
            }

            foreach (var image in imageMap)
            {
                var originalBody = body;
                body = body.Replace(image.Key, image.Value);
                if (body == originalBody)
                {
                    //we have relative image
                    var relativedir = bodyLocation.Substring(0, bodyLocation.LastIndexOf('/'));
                    var relativeImagePath = image.Key.Substring(relativedir.Length + 1);
                    body = body.Replace(relativeImagePath, image.Value);
                }
            }
            foreach (var image in cssMap)
            {
                // body = body.Replace(image.Key, image.Value);
            }
            return body;
        }
        /*
         *  Get the log from the decoding process
         */
        public string getLog()
        {
            return this.log;
        }

        private static string DecodeQuotedPrintables(string input, string charSet)
        {
            if (string.IsNullOrEmpty(charSet))
            {
                var charSetOccurences = new Regex(@"=\?.*\?Q\?", RegexOptions.IgnoreCase);
                var charSetMatches = charSetOccurences.Matches(input);
                foreach (Match match in charSetMatches)
                {
                    charSet = match.Groups[0].Value.Replace("=?", "").Replace("?Q?", "");
                    input = input.Replace(match.Groups[0].Value, "").Replace("?=", "");
                }
            }

            Encoding enc = new ASCIIEncoding();
            if (!string.IsNullOrEmpty(charSet))
            {
                try
                {
                    enc = Encoding.GetEncoding(charSet);
                }
                catch
                {
                    enc = new ASCIIEncoding();
                }
            }

            //decode iso-8859-[0-9]
            var occurences = new Regex(@"=[0-9A-Z]{2}", RegexOptions.Multiline);
            var matches = occurences.Matches(input);
            foreach (Match match in matches)
            {
                try
                {
                    byte[] b = new byte[] { byte.Parse(match.Groups[0].Value.Substring(1), System.Globalization.NumberStyles.AllowHexSpecifier) };
                    char[] hexChar = enc.GetChars(b);
                    input = input.Replace(match.Groups[0].Value, hexChar[0].ToString());
                }
                catch
                {; }
            }

            //decode base64String (utf-8?B?)
            occurences = new Regex(@"\?utf-8\?B\?.*\?", RegexOptions.IgnoreCase);
            matches = occurences.Matches(input);
            foreach (Match match in matches)
            {
                byte[] b = Convert.FromBase64String(match.Groups[0].Value.Replace("?utf-8?B?", "").Replace("?UTF-8?B?", "").Replace("?", ""));
                string temp = Encoding.UTF8.GetString(b);
                input = input.Replace(match.Groups[0].Value, temp);
            }

            input = input.Replace("=\r\n", "");

            return input;
        }

        public string ToSafeFileName(string fileName)
        {
            StringBuilder safe = new StringBuilder();
            var invalidChar = Path.GetInvalidFileNameChars();
            foreach (var c in fileName)
            {
                if (invalidChar.Contains(c))
                    safe.Append("_");
                else
                    safe.Append(c);
            }
            return safe.ToString();
        }

        private class PartInfo
        {
            public String ContentType { get; set; }

            public String ContentName { get; set; }

            public String DataAsString { get; set; }

            public Byte[] DataAsBinary { get; set; }

            public String Location { get; set; }

        }
    }
}
