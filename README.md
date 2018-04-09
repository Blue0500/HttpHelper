# HttpHelper

HttpHelper is a simple library designed to make using `System.Net.Http` simpler and easier. To begin, I focused on reading responses from an `HttpServer`, but I might add additional functionality later. Please note: this is an experiment and I am likely to introduce breaking changes later on.

### MediaType
`MediaType` is a simple class desgined for working with... (you guessed it) web media types such as `text/html` or `application/json`. It's a pretty simple and straightforward class.

````C#
MediaType type = new MediaType("application", "xml");
type = MediaType.Json;

if (MediaType.TryParse("application/javascript; charset=utf-8", out type)) {
    // ...
}

string mainType = type.Type;
string subType = type.SubType;
IDictionary<string, string> parameters = type.Parameters;
````

### HttpResponseReader
`HttpResponseReader` is designed to enable easy and quick processing of a `HttpResponseMessage` from an `HttpClient`. The class works by building a series of functions that first determine if a particular piece of data is present in a message, invoking a callback, and then signaling whether or not message processing should continue. When you read a message, you also provide a result object that gets built as the message is read.

So before we get into the reader itself, we must define a result object that will hold all of the useful data from our message
public class MessageResult {
    public string Html { get; set; }
    public int Code { get; set; }
}
As you can see, we will be retrieving the html content and the response code

Now we must set up our `HttpClient` and get a response
````C#
HttpClient client = new HttpClient();
HttpResponseMessage response = await client.GetAsync("https://github.com");
````

Next, create an `HttpResponseReader` and define what pieces of data should be present. The example below will look for `text/html` content and print the status code. The generic argument of `MessageResult` indicates what the useful data will be stored in.
````C#
var reader = new HttpResponseReader<MessageResult>();

reader.EnsureHtmlContent((string html, MessageResult result) => result.Html = html);
reader.UseResponseCode((HttpStatusCode code, MessageResult result) => result.Code = (int)code);
// The types in the lambda are included for clarity but can be omitted
````

Now all that is left is reading the message! If the message contained the required data, `TryReadMessageAsync` will return true, and false otherwise.
````C#

var result = new MessageResult();
if (await reader.TryReadMessageAsync(response, result)) {
    Console.WriteLine(result.Html);
    Console.WriteLine("Status code: " + result.Code);
}
````

It's as simple as that! Now a few notes: the `UseXXX` methods signal optional processing and will never stop the `HttpResponseReader`, while the `EnsureXXX` methods will definitly stop the reader if the data doesn't match. However, some `UseXXX` methods like `UseResponseCode` signal processing for data that should always be present in a message, like the status code, and so should always invoke their callback.

### Brotli support
Yes that's right! This library allows for decoding brotli-compressed messages (and also gzip and deflate, but those are boring). Simply call the `UseBrotliDecompression` method in the `HttpResponseReader` to decompress the data. Please call any decompression methods before calling content methods, otherwise the decompression will seemingly not function.

Credit: Currently, I'm using a slightly modified internal version of [BrotliSharpLib](https://github.com/master131/BrotliSharpLib) because it is fast and the project is active. Only the library becomes a nuget package I will add it as a proper dependency
