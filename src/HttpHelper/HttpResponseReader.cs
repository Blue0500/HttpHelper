using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.IO.Compression;
using BrotliSharpLib;
using System.Threading.Tasks;

namespace JoshuaKearney.HttpHelper {
    /// <summary>
    ///     A utility class that aids with the reading and processing of a recieved <see cref="HttpResponseMessage"/>
    /// </summary>
    public class HttpResponseReader<T> where T : class {
        private Queue<Func<HttpResponseMessage, T, Task<bool>>> funcs = new Queue<Func<HttpResponseMessage, T, Task<bool>>>();

        // Headers
        /// <summary>
        ///     Ensures that a <see cref="HttpResponseMessage"/> will have a specific header. Message reading is
        ///     halted if this header is not present
        /// </summary>
        /// <param name="headerName">The name of the header</param>
        /// <param name="func">
        ///     A function that is called if the header is successfully found that returns a <see cref="bool"/> that
        ///     determines if message reading should continue
        /// </param>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> EnsureHeader(string headerName, Func<IEnumerable<string>, T, bool> func) {
            this.funcs.Enqueue((message, result) => {
                if (message.Headers.TryGetValues(headerName, out var values)) {
                    return Task.FromResult(func(values, result));
                }
                else {
                    if (message.Content.Headers.TryGetValues(headerName, out values)) {
                        return Task.FromResult(func(values, result));
                    }
                    else {
                        return Task.FromResult(false);
                    }
                }
            });

            return this;
        }

        /// <summary>
        ///     Ensures that a <see cref="HttpResponseMessage"/> will have a specific header. Message reading is
        ///     halted if this header is not present
        /// </summary>
        /// <param name="headerName">The name of the header</param>
        /// <param name="action">An action that is called if the header is successfully found</param>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> EnsureHeader(string headerName, Action<IEnumerable<string>, T> action) {
            return this.EnsureHeader(headerName, (values, result) => {
                action(values ,result);
                return true;
            });
        }

        /// <summary>
        ///     Provides processing for message headers that are optional and may or may not be present. Message
        ///     reading will continue regardless of whether or not the header is found
        /// </summary>
        /// <param name="headerName">The name of the header</param>
        /// <param name="action">An action that is called if the header is successfully found</param>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> UseHeader(string headerName, Action<IEnumerable<string>, T> action) {
            this.funcs.Enqueue((message, result) => {
                if (message.Headers.TryGetValues(headerName, out var values)) {
                    action(values, result);
                }
                else {
                    if (message.Content.Headers.TryGetValues(headerName, out values)) {
                        action(values, result);
                    }                    
                }

                return Task.FromResult(true);
            });

            return this;
        }


        // Content
        /// <summary>
        ///     Ensures that the content of a <see cref="HttpResponseMessage"/> will have a specific type. Message reading will stop
        ///     if the message content is less specific than the given type
        /// </summary>
        /// <param name="type">The content type to match</param>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> EnsureContentType(MediaType type) {
            this.funcs.Enqueue((message, _) => {
                return Task.FromResult(MediaType.FromHeaderValue(message.Content.Headers.ContentType).IsMoreSpecific(type));
            });

            return this;
        }

        /// <summary>
        ///     Provides processing for any content type. Message reading will always continue
        /// </summary>
        /// <param name="action">An action that is invoked with the current message's content type</param>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> UseContentType(Action<MediaType, T> action) {
            this.funcs.Enqueue((message, result) => {
                action(MediaType.FromHeaderValue(message.Content.Headers.ContentType), result);

                return Task.FromResult(true);
            });

            return this;
        }

        /// <summary>
        ///     Ensures that the content of the current message is html/text. Message reading will stop if the content is any other type
        /// </summary>
        /// <param name="action">An action that is invoked with the HTML content</param>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> EnsureHtmlContent(Action<string, T> action) {
            this.EnsureContentType(MediaType.Html);

            this.funcs.Enqueue((message, result) => {
                string str = message.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                action(str, result);
                return Task.FromResult(true);
            });

            return this;
        }

        /// <summary>
        ///     Ensures that the content of the current message is application/json. Message reading will stop if the content is any other type
        /// </summary>
        /// <param name="action">An action that is invoked with the Json content</param>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> EnsureJsonContent(Action<JObject, T> action) {
            this.EnsureContentType(MediaType.Json);

            this.funcs.Enqueue(async (message, result) => {
                string str = await message.Content.ReadAsStringAsync();

                try {
                    var json = JObject.Parse(str);
                    action(json, result);
                    return true;
                }
                catch {
                    return false;
                }
            });

            return this;
        }

        /// <summary>
        ///     Ensures that the content of the current message is application/json. Message reading will stop if the content is any other type
        ///     If the Json content cannot be converted to the type <see cref="T"/>, message reading will stop
        /// </summary>
        /// <param name="action">An action that is invoked with the HTML content</param>
        /// <typeparam name="T">The type that is represented by the json content</param>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> EnsureJsonContent<TJson>(Action<TJson, T> action) {
            this.EnsureContentType(MediaType.Json);

            this.funcs.Enqueue(async (message, result) => {
                string str = await message.Content.ReadAsStringAsync();

                try {
                    var json = JsonConvert.DeserializeObject<TJson>(str);
                    action(json, result);
                    return true;
                }
                catch {
                    return false;
                }
            });

            return this;
        }

        /// <summary>
        ///     Ensures that the content of the current message is text/*. Message reading will stop if the content is any other type
        /// </summary>
        /// <param name="action">An action that is invoked with the plain text content</param>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> EnsureTextContent(Action<string, T> action) {
            this.funcs.Enqueue((message, _) => {
                var media = MediaType.FromHeaderValue(message.Content.Headers.ContentType);
                return Task.FromResult(media.Type.ToLower() == "text");
            });

            this.funcs.Enqueue(async (message, result) => {
                string str = await message.Content.ReadAsStringAsync();
                action(str, result);
                return true;
            });

            return this;
        }

        /// <summary>
        ///     Ensures that the content of the current message is */xml. Message reading will stop if the content is any other type
        /// </summary>
        /// <param name="action">An action that is invoked with the Xml content</param>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> EnsureXmlContent(Action<XDocument, T> action) {
            this.funcs.Enqueue((message, _) => {
                var media = MediaType.FromHeaderValue(message.Content.Headers.ContentType);
                return Task.FromResult(media.SubType.ToLower() == "xml");
            });

            this.funcs.Enqueue(async (message, result) => {
                string str = await message.Content.ReadAsStringAsync();
                
                try {
                    var xml = XDocument.Parse(str);
                    action(xml, result);
                    return true;
                }
                catch {
                    return false;
                }
            });

            return this;
        }

        /// <summary>
        ///     Provides processing for any type of content. Message reading will always continue
        /// </summary>
        /// <param name="action">An action that is invoked with content's type and data</param>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> UseContent(Action<MediaType, byte[], T> action) {
            this.funcs.Enqueue(async (message, result) => {
                var bytes = await message.Content.ReadAsByteArrayAsync();
                action(MediaType.FromHeaderValue(message.Content.Headers.ContentType), bytes, result);
                return true;
            });

            return this;
        }


        // Compression
        /// <summary>
        ///     Automatically decompresses the current message's content if encoded with the Brotli compression algorithm
        /// </summary>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> UseBrotliDecompression() {
            this.funcs.Enqueue(async (message, _) => {
                if (message.Content.Headers.ContentEncoding.FirstOrDefault() == "br") {
                    byte[] raw = await message.Content.ReadAsByteArrayAsync();
                    byte[] decompressed = Brotli.DecompressBuffer(raw, 0, raw.Length);

                    var newContent = new ByteArrayContent(decompressed);
                    newContent.Headers.Clear();

                    TransferHeaders(message.Content, newContent);

                    message.Content.Dispose();
                    message.Content = newContent;
                }

                return true;
            });

            return this;
        }

        /// <summary>
        ///     Automatically decompresses the current message's content if encoded with the brotli Gzip algorithm
        /// </summary>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> UseGzipDecompression() {
            this.funcs.Enqueue(async (message, _) => {
                if (message.Content.Headers.ContentEncoding.FirstOrDefault() == "gzip") {
                    MemoryStream decompressed = new MemoryStream();

                    using (Stream content = await message.Content.ReadAsStreamAsync()) {
                        using (GZipStream gzip = new GZipStream(content, CompressionMode.Decompress)) {
                            await gzip.CopyToAsync(decompressed);
                        }
                    }

                    decompressed.Seek(0, SeekOrigin.Begin);

                    var newContent = new StreamContent(decompressed);
                    newContent.Headers.Clear();

                    TransferHeaders(message.Content, newContent);

                    message.Content.Dispose();
                    message.Content = newContent;
                }

                return true;
            });

            return this;
        }

        /// <summary>
        ///     Automatically decompresses the current message's content if encoded with the Deflate compression algorithm
        /// </summary>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> UseDeflateDecompression() {
            this.funcs.Enqueue(async (message, _) => {
                if (message.Content.Headers.ContentEncoding.FirstOrDefault() == "deflate") {
                    MemoryStream decompressed = new MemoryStream();

                    using (Stream content = await message.Content.ReadAsStreamAsync()) {
                        using (DeflateStream deflate = new DeflateStream(content, CompressionMode.Decompress)) {
                            await deflate.CopyToAsync(decompressed);
                        }
                    }

                    decompressed.Seek(0, SeekOrigin.Begin);

                    var newContent = new StreamContent(decompressed);
                    newContent.Headers.Clear();

                    TransferHeaders(message.Content, newContent);

                    message.Content.Dispose();
                    message.Content = newContent;
                }

                return true;
            });

            return this;
        }


        // General
        /// <summary>
        ///     Provides processing for the <see cref="HttpStatusCode"/> of the current message. Message reading will
        ///     always continue
        /// </summary>
        /// <param name="action">An action that is invoked with the message's status code</param>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> UseResponseCode(Action<HttpStatusCode, T> action) {
            this.funcs.Enqueue((message, result) => {
                action(message.StatusCode, result);
                return Task.FromResult(true);
            });

            return this;
        }

        /// <summary>
        ///     Provides processing for the reason phrase of the message. Message reading will always continue
        /// </summary>
        /// <param name="action">An action that is invoked with the message's reason phrase</param>
        /// <returns>This <see cref="HttpResponseReader"/></returns>
        public HttpResponseReader<T> UseReasonPhrase(Action<string, T> action) {
            this.funcs.Enqueue((message, result) => {
                action(message.ReasonPhrase, result);
                return Task.FromResult(true);
            });

            return this;
        }

        /// <summary>
        ///     Attempts to read the given message using the rules defined by the methods in this class. If any
        ///     fail, message reading will stop. The order of execution is not garunteed
        /// </summary>
        /// <param name="message">The message to process</param>
        /// <param name="shortCircuit">
        ///     Indicates whether or not all message reading should stop if one rule fails. 
        /// </param>
        /// <returns>A boolean indicating whether or not all rules were successfully applied to the message</returns>
        public async Task<bool> TryReadMessageAsync(HttpResponseMessage message, T result, bool shortCircuit = true) {
            bool success = true;

            while (this.funcs.Count > 0) {
                success &= await this.funcs.Dequeue()(message, result);

                if (!success && shortCircuit) {
                    break;
                }
            }

            return success;
        }

        private void TransferHeaders(HttpContent source, HttpContent sink) {
            foreach (var header in source.Headers) {
                sink.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }
}