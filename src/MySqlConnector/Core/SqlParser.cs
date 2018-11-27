using System;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	internal abstract class SqlParser
	{
		public void Parse(string sql)
		{
			OnBeforeParse(sql ?? throw new ArgumentNullException(nameof(sql)));

			int parameterStartIndex = -1;

			var state = State.Beginning;
			var beforeCommentState = State.Beginning;
			bool isNamedParameter = false;
			for (int index = 0; index <= sql.Length; index++)
			{
				char ch = index == sql.Length ? ';' : sql[index];
				if (state == State.EndOfLineComment)
				{
					if (ch == '\n')
						state = beforeCommentState;
				}
				else if (state == State.CStyleComment)
				{
					if (ch == '*')
						state = State.CStyleCommentAsterisk;
				}
				else if (state == State.CStyleCommentAsterisk)
				{
					state = ch == '/' ? beforeCommentState : State.CStyleComment;
				}
				else if (state == State.SingleQuotedString)
				{
					if (ch == '\'')
						state = State.SingleQuotedStringSingleQuote;
					else if (ch == '\\')
						state = State.SingleQuotedStringBackslash;
				}
				else if (state == State.SingleQuotedStringBackslash)
				{
					state = State.SingleQuotedString;
				}
				else if (state == State.DoubleQuotedString)
				{
					if (ch == '"')
						state = State.DoubleQuotedStringDoubleQuote;
					else if (ch == '\\')
						state = State.DoubleQuotedStringBackslash;
				}
				else if (state == State.DoubleQuotedStringBackslash)
				{
					state = State.DoubleQuotedString;
				}
				else if (state == State.BacktickQuotedString)
				{
					if (ch == '`')
						state = State.BacktickQuotedStringBacktick;
				}
				else if (state == State.SingleQuotedStringSingleQuote)
				{
					if (ch == '\'')
					{
						state = State.SingleQuotedString;
					}
					else
					{
						if (isNamedParameter)
							OnNamedParameter(parameterStartIndex, index - parameterStartIndex);
						state = State.Statement;
					}
				}
				else if (state == State.DoubleQuotedStringDoubleQuote)
				{
					if (ch == '"')
					{
						state = State.DoubleQuotedString;
					}
					else
					{
						if (isNamedParameter)
							OnNamedParameter(parameterStartIndex, index - parameterStartIndex);
						state = State.Statement;
					}
				}
				else if (state == State.BacktickQuotedStringBacktick)
				{
					if (ch == '`')
					{
						state = State.BacktickQuotedString;
					}
					else
					{
						if (isNamedParameter)
							OnNamedParameter(parameterStartIndex, index - parameterStartIndex);
						state = State.Statement;
					}
				}
				else if (state == State.SecondHyphen)
				{
					if (ch == ' ')
					{
						state = State.EndOfLineComment;
					}
					else
					{
						state = State.Statement;
					}
				}
				else if (state == State.Hyphen)
				{
					state = ch == '-' ? State.SecondHyphen : State.Statement;
				}
				else if (state == State.ForwardSlash)
				{
					state = ch == '*' ? State.CStyleComment : State.Statement;
				}
				else if (state == State.QuestionMark)
				{
					if (IsVariableName(ch))
					{
						state = State.NamedParameter;
					}
					else
					{
						OnPositionalParameter(parameterStartIndex);
						if (ch == ';')
						{
							OnStatementEnd(index);
							state = State.Beginning;
						}
						else
						{
							state = State.Statement;
						}
					}
				}
				else if (state == State.AtSign)
				{
					if (IsVariableName(ch))
					{
						state = State.NamedParameter;
					}
					else if (ch == '`')
					{
						state = State.BacktickQuotedString;
						isNamedParameter = true;
					}
					else if (ch == '"')
					{
						state = State.DoubleQuotedString;
						isNamedParameter = true;
					}
					else if (ch == '\'')
					{
						state = State.SingleQuotedString;
						isNamedParameter = true;
					}
					else
					{
						state = State.Statement;
					}
				}
				else if (state == State.NamedParameter)
				{
					if (!IsVariableName(ch))
					{
						OnNamedParameter(parameterStartIndex, index - parameterStartIndex);
						if (ch == ';')
						{
							OnStatementEnd(index);
							state = State.Beginning;
						}
						else
						{
							state = State.Statement;
						}
					}
				}
				else
				{
					if (state != State.Beginning && state != State.Statement)
						throw new InvalidOperationException("Unexpected state: {0}".FormatInvariant(state));

					if (ch == '-' && index < sql.Length - 2 && sql[index + 1] == '-' && sql[index + 2] == ' ')
					{
						beforeCommentState = state;
						state = State.Hyphen;
					}
					else if (ch == '/' && index < sql.Length - 1 && sql[index + 1] == '*')
						state = State.ForwardSlash;
					else if (ch == '\'')
						state = State.SingleQuotedString;
					else if (ch == '"')
						state = State.DoubleQuotedString;
					else if (ch == '`')
						state = State.BacktickQuotedString;
					else if (ch == '?')
					{
						state = State.QuestionMark;
						parameterStartIndex = index;
					}
					else if (ch == '@')
					{
						state = State.AtSign;
						parameterStartIndex = index;
					}
					else if (ch == '#')
					{
						beforeCommentState = state;
						state = State.EndOfLineComment;
					}
					else if (ch == ';')
					{
						if (state != State.Beginning)
							OnStatementEnd(index);
						state = State.Beginning;
					}
					else if (!IsWhitespace(ch) && state == State.Beginning)
					{
						state = State.Statement;
						OnStatementBegin(index);
					}
				}
			}

			OnParsed();
		}

		protected virtual void OnBeforeParse(string sql)
		{
		}

		protected virtual void OnStatementBegin(int index)
		{
		}

		protected virtual void OnPositionalParameter(int index)
		{
		}

		protected virtual void OnNamedParameter(int index, int length)
		{
		}

		protected virtual void OnStatementEnd(int index)
		{
		}

		protected virtual void OnParsed()
		{
		}

		private static bool IsWhitespace(char ch) => ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';

		private static bool IsVariableName(char ch) => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '.' || ch == '_' || ch == '$' || (ch >= 0x0080 && ch <= 0xFFFF);

		private enum State
		{
			Beginning,
			Statement,
			SingleQuotedString,
			SingleQuotedStringBackslash,
			SingleQuotedStringSingleQuote,
			DoubleQuotedString,
			DoubleQuotedStringBackslash,
			DoubleQuotedStringDoubleQuote,
			BacktickQuotedString,
			BacktickQuotedStringBacktick,
			EndOfLineComment,
			Hyphen,
			SecondHyphen,
			ForwardSlash,
			CStyleComment,
			CStyleCommentAsterisk,
			QuestionMark,
			AtSign,
			NamedParameter,
		}
	}
}
