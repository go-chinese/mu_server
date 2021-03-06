﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MILogin
{
	
	public class MUJson
	{
		
		public static object jsonDecode(string json)
		{
			MUJson.lastDecode = json;
			object result;
			try
			{
				if (json != null)
				{
					char[] charArray = json.ToCharArray();
					int index = 0;
					bool success = true;
					object value = MUJson.parseValue(charArray, ref index, ref success);
					if (success)
					{
						MUJson.lastErrorIndex = -1;
					}
					else
					{
						MUJson.lastErrorIndex = index;
					}
					result = value;
				}
				else
				{
					result = null;
				}
			}
			catch (Exception)
			{
				result = null;
			}
			return result;
		}

		
		public static string jsonEncode(object json)
		{
			StringBuilder builder = new StringBuilder(2000);
			return MUJson.serializeValue(json, builder) ? builder.ToString() : null;
		}

		
		public static bool lastDecodeSuccessful()
		{
			return MUJson.lastErrorIndex == -1;
		}

		
		public static int getLastErrorIndex()
		{
			return MUJson.lastErrorIndex;
		}

		
		public static string getLastErrorSnippet()
		{
			string result;
			if (MUJson.lastErrorIndex == -1)
			{
				result = "";
			}
			else
			{
				int startIndex = MUJson.lastErrorIndex - 5;
				int endIndex = MUJson.lastErrorIndex + 15;
				if (startIndex < 0)
				{
					startIndex = 0;
				}
				if (endIndex >= MUJson.lastDecode.Length)
				{
					endIndex = MUJson.lastDecode.Length - 1;
				}
				result = MUJson.lastDecode.Substring(startIndex, endIndex - startIndex + 1);
			}
			return result;
		}

		
		protected static Hashtable parseObject(char[] json, ref int index)
		{
			Hashtable table = new Hashtable();
			MUJson.nextToken(json, ref index);
			bool done = false;
			while (!done)
			{
				int token = MUJson.lookAhead(json, index);
				if (token != 0)
				{
					if (token == 6)
					{
						MUJson.nextToken(json, ref index);
					}
					else
					{
						if (token == 2)
						{
							MUJson.nextToken(json, ref index);
							return table;
						}
						string name = MUJson.parseString(json, ref index);
						if (name == null)
						{
							return null;
						}
						token = MUJson.nextToken(json, ref index);
						if (token != 5)
						{
							return null;
						}
						bool success = true;
						object value = MUJson.parseValue(json, ref index, ref success);
						if (!success)
						{
							return null;
						}
						table[name] = value;
					}
					continue;
				}
				return null;
			}
			return table;
		}

		
		protected static ArrayList parseArray(char[] json, ref int index)
		{
			ArrayList array = new ArrayList();
			MUJson.nextToken(json, ref index);
			bool done = false;
			while (!done)
			{
				int token = MUJson.lookAhead(json, index);
				if (token != 0)
				{
					if (token == 6)
					{
						MUJson.nextToken(json, ref index);
					}
					else
					{
						if (token == 4)
						{
							MUJson.nextToken(json, ref index);
							break;
						}
						bool success = true;
						object value = MUJson.parseValue(json, ref index, ref success);
						if (!success)
						{
							return null;
						}
						array.Add(value);
					}
					continue;
				}
				return null;
			}
			return array;
		}

		
		protected static object parseValue(char[] json, ref int index, ref bool success)
		{
			switch (MUJson.lookAhead(json, index))
			{
			case 1:
				return MUJson.parseObject(json, ref index);
			case 3:
				return MUJson.parseArray(json, ref index);
			case 7:
				return MUJson.parseString(json, ref index);
			case 8:
				return MUJson.parseNumber(json, ref index);
			case 9:
				MUJson.nextToken(json, ref index);
				return bool.Parse("TRUE");
			case 10:
				MUJson.nextToken(json, ref index);
				return bool.Parse("FALSE");
			case 11:
				MUJson.nextToken(json, ref index);
				return null;
			}
			success = false;
			return null;
		}

		
		protected static string parseString(char[] json, ref int index)
		{
			string s = "";
			MUJson.eatWhitespace(json, ref index);
			char c = json[index++];
			bool complete = false;
			while (!complete)
			{
				if (index == json.Length)
				{
					break;
				}
				c = json[index++];
				if (c == '"')
				{
					complete = true;
					break;
				}
				if (c == '\\')
				{
					if (index == json.Length)
					{
						break;
					}
					c = json[index++];
					if (c == '"')
					{
						s += '"';
					}
					else if (c == '\\')
					{
						s += '\\';
					}
					else if (c == '/')
					{
						s += '/';
					}
					else if (c == 'b')
					{
						s += '\b';
					}
					else if (c == 'f')
					{
						s += '\f';
					}
					else if (c == 'n')
					{
						s += '\n';
					}
					else if (c == 'r')
					{
						s += '\r';
					}
					else if (c == 't')
					{
						s += '\t';
					}
					else if (c == 'u')
					{
						int remainingLength = json.Length - index;
						if (remainingLength < 4)
						{
							break;
						}
						char[] unicodeCharArray = new char[4];
						Array.Copy(json, index, unicodeCharArray, 0, 4);
						s = s + "&#x" + new string(unicodeCharArray) + ";";
						index += 4;
					}
				}
				else
				{
					s += c;
				}
			}
			string result;
			if (!complete)
			{
				result = null;
			}
			else
			{
				result = s;
			}
			return result;
		}

		
		protected static double parseNumber(char[] json, ref int index)
		{
			MUJson.eatWhitespace(json, ref index);
			int lastIndex = MUJson.getLastIndexOfNumber(json, index);
			int charLength = lastIndex - index + 1;
			char[] numberCharArray = new char[charLength];
			Array.Copy(json, index, numberCharArray, 0, charLength);
			index = lastIndex + 1;
			return double.Parse(new string(numberCharArray));
		}

		
		protected static int getLastIndexOfNumber(char[] json, int index)
		{
			int lastIndex;
			for (lastIndex = index; lastIndex < json.Length; lastIndex++)
			{
				if ("0123456789+-.eE".IndexOf(json[lastIndex]) == -1)
				{
					break;
				}
			}
			return lastIndex - 1;
		}

		
		protected static void eatWhitespace(char[] json, ref int index)
		{
			while (index < json.Length)
			{
				if (" \t\n\r".IndexOf(json[index]) == -1)
				{
					break;
				}
				index++;
			}
		}

		
		protected static int lookAhead(char[] json, int index)
		{
			int saveIndex = index;
			return MUJson.nextToken(json, ref saveIndex);
		}

		
		protected static int nextToken(char[] json, ref int index)
		{
			MUJson.eatWhitespace(json, ref index);
			int result;
			if (index == json.Length)
			{
				result = 0;
			}
			else
			{
				char c = json[index];
				index++;
				char c2 = c;
				switch (c2)
				{
				case '"':
					return 7;
				case '#':
				case '$':
				case '%':
				case '&':
				case '\'':
				case '(':
				case ')':
				case '*':
				case '+':
				case '.':
				case '/':
					break;
				case ',':
					return 6;
				case '-':
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					return 8;
				case ':':
					return 5;
				default:
					switch (c2)
					{
					case '[':
						return 3;
					case '\\':
						break;
					case ']':
						return 4;
					default:
						switch (c2)
						{
						case '{':
							return 1;
						case '}':
							return 2;
						}
						break;
					}
					break;
				}
				index--;
				int remainingLength = json.Length - index;
				if (remainingLength >= 5)
				{
					if (json[index] == 'f' && json[index + 1] == 'a' && json[index + 2] == 'l' && json[index + 3] == 's' && json[index + 4] == 'e')
					{
						index += 5;
						return 10;
					}
				}
				if (remainingLength >= 4)
				{
					if (json[index] == 't' && json[index + 1] == 'r' && json[index + 2] == 'u' && json[index + 3] == 'e')
					{
						index += 4;
						return 9;
					}
				}
				if (remainingLength >= 4)
				{
					if (json[index] == 'n' && json[index + 1] == 'u' && json[index + 2] == 'l' && json[index + 3] == 'l')
					{
						index += 4;
						return 11;
					}
				}
				result = 0;
			}
			return result;
		}

		
		protected static bool serializeObjectOrArray(object objectOrArray, StringBuilder builder)
		{
			bool result;
			if (objectOrArray is Hashtable)
			{
				result = MUJson.serializeObject((Hashtable)objectOrArray, builder);
			}
			else
			{
				result = (objectOrArray is ArrayList && MUJson.serializeArray((ArrayList)objectOrArray, builder));
			}
			return result;
		}

		
		protected static bool serializeObject(Hashtable anObject, StringBuilder builder)
		{
			builder.Append("{");
			IDictionaryEnumerator e = anObject.GetEnumerator();
			bool first = true;
			while (e.MoveNext())
			{
				string key = e.Key.ToString();
				object value = e.Value;
				if (!first)
				{
					builder.Append(", ");
				}
				MUJson.serializeString(key, builder);
				builder.Append(":");
				if (!MUJson.serializeValue(value, builder))
				{
					return false;
				}
				first = false;
			}
			builder.Append("}");
			return true;
		}

		
		protected static bool serializeDictionary(Dictionary<string, string> dict, StringBuilder builder)
		{
			builder.Append("{");
			bool first = true;
			foreach (KeyValuePair<string, string> kv in dict)
			{
				if (!first)
				{
					builder.Append(", ");
				}
				MUJson.serializeString(kv.Key, builder);
				builder.Append(":");
				MUJson.serializeString(kv.Value, builder);
				first = false;
			}
			builder.Append("}");
			return true;
		}

		
		protected static bool serializeArray(ArrayList anArray, StringBuilder builder)
		{
			builder.Append("[");
			bool first = true;
			for (int i = 0; i < anArray.Count; i++)
			{
				object value = anArray[i];
				if (!first)
				{
					builder.Append(", ");
				}
				if (!MUJson.serializeValue(value, builder))
				{
					return false;
				}
				first = false;
			}
			builder.Append("]");
			return true;
		}

		
		protected static bool serializeValue(object value, StringBuilder builder)
		{
			if (value == null)
			{
				builder.Append("null");
			}
			else if (value.GetType().IsArray)
			{
				MUJson.serializeArray(new ArrayList((ICollection)value), builder);
			}
			else if (value is string)
			{
				MUJson.serializeString((string)value, builder);
			}
			else if (value is char)
			{
				MUJson.serializeString(Convert.ToString((char)value), builder);
			}
			else if (value is Hashtable)
			{
				MUJson.serializeObject((Hashtable)value, builder);
			}
			else if (value is Dictionary<string, string>)
			{
				MUJson.serializeDictionary((Dictionary<string, string>)value, builder);
			}
			else if (value is ArrayList)
			{
				MUJson.serializeArray((ArrayList)value, builder);
			}
			else if (value is bool && (bool)value)
			{
				builder.Append("true");
			}
			else if (value is bool && !(bool)value)
			{
				builder.Append("false");
			}
			else
			{
				if (!value.GetType().IsPrimitive)
				{
					return false;
				}
				MUJson.serializeNumber(Convert.ToDouble(value), builder);
			}
			return true;
		}

		
		protected static void serializeString(string aString, StringBuilder builder)
		{
			builder.Append("\"");
			foreach (char c in aString.ToCharArray())
			{
				if (c == '"')
				{
					builder.Append("\\\"");
				}
				else if (c == '\\')
				{
					builder.Append("\\\\");
				}
				else if (c == '\b')
				{
					builder.Append("\\b");
				}
				else if (c == '\f')
				{
					builder.Append("\\f");
				}
				else if (c == '\n')
				{
					builder.Append("\\n");
				}
				else if (c == '\r')
				{
					builder.Append("\\r");
				}
				else if (c == '\t')
				{
					builder.Append("\\t");
				}
				else
				{
					int codepoint = Convert.ToInt32(c);
					if (codepoint >= 32 && codepoint <= 126)
					{
						builder.Append(c);
					}
					else
					{
						builder.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
					}
				}
			}
			builder.Append("\"");
		}

		
		protected static void serializeNumber(double number, StringBuilder builder)
		{
			builder.Append(Convert.ToString(number));
		}

		
		private const int TOKEN_NONE = 0;

		
		private const int TOKEN_CURLY_OPEN = 1;

		
		private const int TOKEN_CURLY_CLOSE = 2;

		
		private const int TOKEN_SQUARED_OPEN = 3;

		
		private const int TOKEN_SQUARED_CLOSE = 4;

		
		private const int TOKEN_COLON = 5;

		
		private const int TOKEN_COMMA = 6;

		
		private const int TOKEN_STRING = 7;

		
		private const int TOKEN_NUMBER = 8;

		
		private const int TOKEN_TRUE = 9;

		
		private const int TOKEN_FALSE = 10;

		
		private const int TOKEN_NULL = 11;

		
		private const int BUILDER_CAPACITY = 2000;

		
		protected static int lastErrorIndex = -1;

		
		protected static string lastDecode = "";
	}
}
