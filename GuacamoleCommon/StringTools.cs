using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuacamoleClient.Common
{
    internal class StringTools
    {
        /// <summary>
        /// Add/remove indentation
        /// </summary>
        /// <param name="text"></param>
        /// <param name="spaceCountForIndentation"></param>
        /// <param name="lineBreak">The line break type which has been used in text</param>
        /// <returns></returns>
        public static string IndentString(string text, int spaceCountForIndentation, string lineBreak)
        {
            if (string.IsNullOrEmpty(text) || spaceCountForIndentation == 0)
                return text;
            else if (spaceCountForIndentation > 0)
                return Strings.Space(spaceCountForIndentation) + text.Replace(lineBreak, lineBreak + Strings.Space(spaceCountForIndentation));
            else if (text.StartsWith(Strings.Space(spaceCountForIndentation)))
                // remove indentation at begin of text
                return text.Substring(spaceCountForIndentation).Replace(lineBreak + Strings.Space(spaceCountForIndentation), lineBreak);
            else
                // begin of text is not indented
                return text.Replace(lineBreak + Strings.Space(spaceCountForIndentation), lineBreak);
        }

        /// <summary>
        /// Add/remove indentation
        /// </summary>
        /// <param name="text"></param>
        /// <param name="spaceCountForIndentation"></param>
        /// <param name="lineBreak">The line break type which has been used in text</param>
        /// <returns></returns>
        public static string IndentStringStartingWith2ndLine(string text, int spaceCountForIndentation, string lineBreak)
        {
            if (string.IsNullOrEmpty(text) || spaceCountForIndentation == 0)
                return text;
            else if (spaceCountForIndentation > 0)
                return text.Replace(lineBreak, lineBreak + Strings.Space(spaceCountForIndentation));
            else
                return text.Replace(lineBreak + Strings.Space(spaceCountForIndentation), lineBreak);
        }

        public enum NewLineTypeSource : byte
        {
            EnvironmentNewLine = 0,
            Cr = 1,
            Lf = 2,
            CrLf = 3,
            All = 10
        }

        public enum NewLineTypeDestination : byte
        {
            EnvironmentNewLine = 0,
            Cr = 1,
            Lf = 2,
            CrLf = 3
        }

        /// <summary>
        /// Convert a text with some line-breaks into a targetted line-break formatting
        /// </summary>
        /// <param name="text"></param>
        /// <param name="existingNewLineTypes"></param>
        /// <param name="targetNewLineType"></param>
        /// <returns></returns>
        public static string ConvertNewLineType(string text, NewLineTypeSource existingNewLineTypes, NewLineTypeDestination targetNewLineType)
        {
            string TargetNewLine;
            switch (targetNewLineType)
            {
                case NewLineTypeDestination.EnvironmentNewLine:
                    {
                        TargetNewLine = Environment.NewLine;
                        break;
                    }

                case NewLineTypeDestination.Cr:
                    {
                        TargetNewLine = "\r";
                        break;
                    }

                case NewLineTypeDestination.Lf:
                    {
                        TargetNewLine = "\n";
                        break;
                    }

                case NewLineTypeDestination.CrLf:
                    {
                        TargetNewLine = "\r\n";
                        break;
                    }

                default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(targetNewLineType));
                    }
            }

            switch (existingNewLineTypes)
            {
                case NewLineTypeSource.EnvironmentNewLine:
                    {
                        return text.Replace(Environment.NewLine, TargetNewLine);
                    }

                case NewLineTypeSource.Cr:
                    {
                        return text.Replace("\r", TargetNewLine);
                    }

                case NewLineTypeSource.Lf:
                    {
                        return text.Replace("\n", TargetNewLine);
                    }

                case NewLineTypeSource.CrLf:
                    {
                        return text.Replace("\r\n", TargetNewLine);
                    }

                case NewLineTypeSource.All:
                    {
                        //return text.Replace(ControlChars.CrLf, ControlChars.Lf).Replace(ControlChars.Cr, ControlChars.Lf).Replace(ControlChars.Lf, TargetNewLine);
                        return text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", TargetNewLine);
                    }

                default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(existingNewLineTypes));
                    }
            }
        }
    }
}
