#pragma warning disable SA1520 // Use braces consistently

namespace MySqlConnector.Core;

internal abstract class SqlParser(StatementPreparer preparer)
{
	protected StatementPreparer Preparer { get; } = preparer;

	public void Parse(string sql)
	{
		ArgumentNullException.ThrowIfNull(sql);
		OnBeforeParse(sql);

		int parameterStartIndex = -1;
		var noBackslashEscapes = (Preparer.Options & StatementPreparerOptions.NoBackslashEscapes) == StatementPreparerOptions.NoBackslashEscapes;

		var state = State.Beginning;
		var beforeCommentState = State.Beginning;
		var isNamedParameter = false;
		for (var index = 0; index < sql.Length; index++)
		{
			var ch = sql[index];
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
				else if (ch == '\\' && !noBackslashEscapes)
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
				else if (ch == '\\' && !noBackslashEscapes)
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
				if (state is not State.Beginning and not State.Statement)
					throw new InvalidOperationException($"Unexpected state: {state}");

				if (ch == '-' && index < sql.Length - 2 && sql[index + 1] == '-' && sql[index + 2] == ' ')
				{
					beforeCommentState = state;
					state = State.Hyphen;
				}
				else if (ch == '/' && index < sql.Length - 1 && sql[index + 1] == '*')
				{
					beforeCommentState = state;
					state = State.ForwardSlash;
				}
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

		var states = FinalParseStates.None;
		if (state == State.NamedParameter)
		{
			OnNamedParameter(parameterStartIndex, sql.Length - parameterStartIndex);
			state = State.Statement;
		}
		else if (state == State.QuestionMark)
		{
			OnPositionalParameter(parameterStartIndex);
			state = State.Statement;
		}
		else if (state == State.EndOfLineComment)
		{
			states |= FinalParseStates.NeedsNewline;
			state = beforeCommentState;
		}
		else if (state is State.SingleQuotedStringSingleQuote or State.DoubleQuotedStringDoubleQuote or State.BacktickQuotedStringBacktick)
		{
			state = State.Statement;
		}

		if (state == State.Statement)
		{
			OnStatementEnd(sql.Length);
			states |= FinalParseStates.NeedsSemicolon;
			state = State.Beginning;
		}
		if (state == State.Beginning)
		{
			states |= FinalParseStates.Complete;
		}

		OnParsed(states);
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

	protected virtual void OnParsed(FinalParseStates states)
	{
	}

	[Flags]
	protected enum FinalParseStates
	{
		None = 0,

		/// <summary>
		/// The statement is complete (apart from potentially needing a semicolon or newline).
		/// </summary>
		Complete = 1,

		/// <summary>
		/// The statement needs a newline (e.g., to terminate a final comment).
		/// </summary>
		NeedsNewline = 2,

		/// <summary>
		/// The statement needs a semicolon (if another statement is going to be concatenated to it).
		/// </summary>
		NeedsSemicolon = 4,
	}

	private static bool IsWhitespace(char ch) => ch is ' ' or '\t' or '\r' or '\n';

	private static bool IsVariableName(char ch) => ch is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '.' or '_' or '$' or (>= (char) 0x0080 and <= (char) 0xFFFF);

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
