using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace JoshuaKearney.HttpHelper {
    /// <summary>
    ///     A simple class that represents web media types
    /// </summary>
    public class MediaType : IEquatable<MediaType>, IComparable<MediaType> {
        /// <summary>
        ///     Represents the application/javascript media type
        /// </summary>
        public static MediaType Javascript { get; } = new MediaType("application", "javascript");

        /// <summary>
        ///     Represents the application/javascript media type
        /// </summary>
        public static MediaType Json { get; } = new MediaType("application", "json");

        /// <summary>
        ///     Represents the application/xml media type
        /// </summary>
        public static MediaType Xml { get; } = new MediaType("application", "xml");

        /// <summary>
        ///     Represents the application/zip media type
        /// </summary>
        public static MediaType Zip { get; } = new MediaType("application", "zip");

        /// <summary>
        ///     Represents the application/pdf media type
        /// </summary>
        public static MediaType Pdf { get; } = new MediaType("application", "pdf");

        /// <summary>
        ///     Represents the application/octet-stream media type
        /// </summary>
        public static MediaType Binary { get; } = new MediaType("application", "octet-stream");

        /// <summary>
        ///     Represents the audio/mpeg media type
        /// </summary>
        public static MediaType Mpeg { get; } = new MediaType("audio", "mpeg");

        /// <summary>
        ///     Represents the audio/vorbis media type
        /// </summary>
        public static MediaType Vorbis { get; } = new MediaType("audio", "vorbis");

        /// <summary>
        ///     Represents the text/css media type
        /// </summary>
        public static MediaType Css { get; } = new MediaType("text", "css");

        /// <summary>
        ///     Represents the text/html media type
        /// </summary>
        public static MediaType Html { get; } = new MediaType("text", "html");

        /// <summary>
        ///     Represents the text/plain media type
        /// </summary>
        public static MediaType Text { get; } = new MediaType("text", "plain");

        /// <summary>
        ///     Represents the text/xml media type
        /// </summary>
        public static MediaType TextXml { get; } = new MediaType("text", "xml");

        /// <summary>
        ///     Represents the image/png media type
        /// </summary>
        public static MediaType Png { get; } = new MediaType("image", "png");

        /// <summary>
        ///     Represents the image/jpeg media type
        /// </summary>
        public static MediaType Jpeg { get; } = new MediaType("image", "jpeg");

        /// <summary>
        ///     Represents the image/gif media type
        /// </summary>
        public static MediaType Gif { get; } = new MediaType("image", "gif");

        /// <summary>
        ///     Attemps to parse the given string in the format of main-type/[prefix.]sub-type[+suffix] [; param=value]*
        /// </summary>
        /// <param name="mediaType">The media type to parse</param>
        /// <param name="type">The parsed result</param>
        /// <returns>A boolean indicating whether or not the parse succeeded</returns>
        public static bool TryParse(string mediaType, out MediaType type) {
            mediaType = mediaType.Replace(" ", "").ToLower();

            if (mediaType == null) {
                throw new ArgumentNullException();
            }

            Func<char, bool> isValidChar = x => char.IsLetter(x) || x == '_' || x == '-' || char.IsDigit(x);

            string[] split = mediaType.Split('/');
            if (split.Length != 2) {
                type = null;
                return false;
            }

            string firstType = split[0];
            if (!firstType.All(isValidChar)) {
                type = null;
                return false;
            }

            split = split[1].Split('.');
            if (split.Length > 2) {
                type = null;
                return false;
            }

            string tree = string.Empty;
            string rest;
            if (split.Length == 2) {
                tree = split[0];
                rest = split[1];

                if (!tree.All(isValidChar)) {
                    type = null;
                    return false;
                }
            }
            else {
                rest = split[0];
            }

            split = rest.Split(';');
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            foreach (string parameter in split.Skip(1)) {
                string[] pSplit = parameter.Split('=');
                if (pSplit.Length != 2) {
                    type = null;
                    return false;
                }

                if (!pSplit[0].All(isValidChar) || !pSplit[1].All(isValidChar)) {
                    type = null;
                    return false;
                }

                parameters.Add(pSplit[0], pSplit[1]);
            }

            split = split[0].Split('+');

            if (split.Length > 2) {
                type = null;
                return false;
            }

            string subType = split[0];
            if (!subType.All(isValidChar)) {
                type = null;
                return false;
            }

            string suffix = string.Empty;
            if (split.Length == 2) {
                suffix = split[1];

                if (!suffix.All(isValidChar)) {
                    type = null;
                    return false;
                }
            }

            type = new MediaType(firstType, subType, tree, suffix, parameters);
            return true;

            // top-level type name / [ tree. ] subtype name [ +suffix ] [ ; parameters ]
        }

        /// <summary>
        ///     Parses the given string in the format of main-type/[prefix.]sub-type[+suffix] [; param=value]*
        /// </summary>
        /// <exception cref="FormatException"></exception>
        /// <param name="mediaType">The media type to parse</param>
        /// <returns>The parsed media type</returns>
        public static MediaType Parse(string mediaType) {
            if (TryParse(mediaType, out var type)) {
                return type;
            }
            else {
                throw new FormatException();
            }
        }

        /// <summary>
        ///     Creates a <see cref="MediaType"/> from the given <see cref="MediaTypeHeaderValue"/>
        /// </summary>
        public static MediaType FromHeaderValue(MediaTypeHeaderValue value) {
            return Parse(value.ToString());
        }

        /// <summary>
        ///     The key=value parameters at the end of a media type. For example: charset=utf-8
        /// </summary>
        public IReadOnlyDictionary<string, string> Parameters { get; }

        /// <summary>
        ///     The main type of media. For example: text in text/html
        /// </summary>
        public string Type { get; }

        /// <summary>
        ///     The sub type of media. For example: html in text/html
        /// </summary>
        public string SubType { get; }

        /// <summary>
        ///     The optional tree of the media type
        /// </summary>
        public string Tree { get; }

        /// <summary>
        ///     The optional suffix of the media type
        /// </summary>
        public string Suffix { get; }

        /// <summary>
        ///     Creates a <see cref="MediaType"/> from the given type and subtype
        /// </summary>
        public MediaType(string type, string subtype) : this(type, subtype, string.Empty, string.Empty, new Dictionary<string, string>()) { }

        /// <summary>
        /// Creates a <see cref="MediaType"/> from the given type, subtype, tree, suffix, and parameters
        /// </summary>
        public MediaType(string type, string subtype, string tree, string suffix, IDictionary<string, string> parameters) {
            if (type == null || tree == null || subtype == null || suffix == null || parameters == null) {
                throw new ArgumentNullException();
            }

            this.Type = type.ToLower();
            this.SubType = subtype.ToLower();
            this.Tree = tree.ToLower();
            this.Suffix = suffix.ToLower();
            this.Parameters = parameters.ToDictionary(x => x.Key.ToLower(), y => y.Value.ToLower());
        }

        public override string ToString() {
            string result = this.Type + "/";

            if (!string.IsNullOrWhiteSpace(this.Tree)) {
                result += this.Tree + ".";
            }

            result += this.SubType;

            if (!string.IsNullOrWhiteSpace(this.Suffix)) {
                result += "+" + this.Suffix;
            }
            
            foreach (var pair in this.Parameters) {
                result += "; " + pair.Key + "=" + pair.Value;
            }

            return result;
        }

        public override bool Equals(object obj) {
            return this.Equals(obj as MediaType);
        }

        public override int GetHashCode() {
            return this.ToString().GetHashCode();
        }

        public bool Equals(MediaType other) {
            if (other == null) {
                return false;
            }

            return this.ToString() == other.ToString();
        }

        public int CompareTo(MediaType other) {
            if (other == null) {
                return -1;
            }

            return this.ToString().CompareTo(other.ToString());
        }

        /// <summary>
        ///     Determines if a media type is a more specific version of another. For exampe: "text/html; charset=utf-8" is more
        ///     specific than "text/html"
        /// </summary>
        public bool IsMoreSpecific(MediaType other) {
            if (this.Type != other.Type || this.SubType != other.SubType) {
                return false;
            }

            if (this.Tree != other.Tree && !string.IsNullOrWhiteSpace(other.Tree)) {
                return false;
            }

            if (this.Suffix != other.Suffix && !string.IsNullOrWhiteSpace(other.Suffix)) {
                return false;
            }

            foreach (var pair in this.Parameters) {
                if (other.Parameters.ContainsKey(pair.Key)) {
                    if (other.Parameters[pair.Key] != pair.Value) {
                        return false;
                    }
                }
            }

            foreach (var pair in other.Parameters) {
                if (!this.Parameters.ContainsKey(pair.Key) || this.Parameters[pair.Key] != pair.Value) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Converts the current <see cref="MediaType"/> to a <see cref="MediaTypeHeaderValue"/>
        /// </summary>
        public MediaTypeHeaderValue ToHeaderValue() {
            return MediaTypeHeaderValue.Parse(this.ToString());
        }

        /// <summary>
        ///     Converts the current <see cref="MediaType"/> to a <see cref="MediaTypeWithQualityHeaderValue"/>
        /// </summary>
        public MediaTypeHeaderValue ToHeaderValueWithQuality() {
            return MediaTypeWithQualityHeaderValue.Parse(this.ToString());
        }

        public static bool operator ==(MediaType type1, MediaType type2) {
            if (type1 == null) {
                return type2 == null;
            }
            else {
                return type1.Equals(type2);
            }
        }

        public static bool operator !=(MediaType type1, MediaType type2) {
            if (type1 == null) {
                return type2 != null;
            }
            else {
                return !type1.Equals(type2);
            }
        }

        public static implicit operator MediaType(MediaTypeHeaderValue value) {
            return FromHeaderValue(value);
        }

        public static implicit operator MediaTypeHeaderValue(MediaType type) {
            return type.ToHeaderValue();
        }
    }
}