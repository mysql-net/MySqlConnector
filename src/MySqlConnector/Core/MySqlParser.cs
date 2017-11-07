using System;

namespace MySql.Data.MySqlClient
{
	internal abstract class MySqlParser
	{
		public int Parse(string sql)
		{
			OnBeforeParse(sql ?? throw new ArgumentNullException(nameof(sql)));

			int statementCount = 0;
			int parameterStartIndex = -1;

			var state = State.Beginning;
			var beforeCommentState = State.Beginning;
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
					if (ch == '/')
						state = beforeCommentState;
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
					state = ch == '\'' ? State.SingleQuotedString : State.Statement;
				}
				else if (state == State.DoubleQuotedStringDoubleQuote)
				{
					state = ch == '"' ? State.DoubleQuotedString : State.Statement;
				}
				else if (state == State.BacktickQuotedStringBacktick)
				{
					state = ch == '`' ? State.BacktickQuotedString : State.Statement;
				}
				else if (state == State.SecondHyphen)
				{
					if (ch == ' ')
					{
						beforeCommentState = state;
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
						state = State.Statement;
					}
				}
				else if (state == State.AtSign)
				{
					state = IsVariableName(ch) ? State.NamedParameter : State.Statement;
				}
				else if (state == State.NamedParameter)
				{
					if (!IsVariableName(ch))
					{
						OnNamedParameter(parameterStartIndex, index - parameterStartIndex);
						state = State.Statement;
					}
				}
				else
				{
					if (state != State.Beginning && state != State.Statement)
						throw new InvalidOperationException("Unexpected state: {0}".FormatInvariant(state));

					if (ch == '-')
						state = State.Hyphen;
					else if (ch == '/')
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
						IncrementStatementCount(state, ref statementCount);
						state = State.Beginning;
					}
					else if (!IsWhitespace(ch) && state == State.Beginning)
					{
						state = State.Statement;
					}
				}
			}

			IncrementStatementCount(state, ref statementCount);
			OnParsed();
			return statementCount;
		}

		protected virtual void OnBeforeParse(string sql)
		{
		}

		protected virtual void OnPositionalParameter(int index)
		{
		}

		protected virtual void OnNamedParameter(int index, int length)
		{
		}

		protected virtual void OnParsed()
		{
		}

		private void IncrementStatementCount(State state, ref int statementCount)
		{
			if (state != State.Beginning && state != State.EndOfLineComment && state != State.CStyleComment && state != State.CStyleCommentAsterisk)
				statementCount++;
		}

		private static bool IsWhitespace(char ch) => ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';

		private static bool IsVariableName(char ch) => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '.' || ch == '_' || ch == '$';

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
