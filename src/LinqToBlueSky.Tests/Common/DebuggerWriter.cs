﻿using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace LinqToBlueSky.Tests.Common;

/// <summary>
/// Implements a <see cref="TextWriter"/> for writing information to the debugger log.
/// </summary>
/// <seealso cref="Debugger.Log"/>
/// <remarks>This code is created by Kris Vandermotten</remarks>
/// /// <remarks>For more information check this link: http://www.u2u.info/Blogs/Kris/Lists/Posts/Post.aspx?ID=11 </remarks>
public class DebuggerWriter : TextWriter
{
    private bool isOpen;
    private static UnicodeEncoding encoding;
    private readonly int level;
    private readonly string category;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebuggerWriter"/> class.
    /// </summary>
    public DebuggerWriter()
#if NETCORE
        : this(0, string.Empty)
#else
        : this(0, Debugger.DefaultCategory)
#endif
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DebuggerWriter"/> class with the specified level and category.
    /// </summary>
    /// <param name="level">A description of the importance of the messages.</param>
    /// <param name="category">The category of the messages.</param>
    public DebuggerWriter(int level, string category)
        : this(level, category, CultureInfo.CurrentCulture)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DebuggerWriter"/> class with the specified level, category and format provider.
    /// </summary>
    /// <param name="level">A description of the importance of the messages.</param>
    /// <param name="category">The category of the messages.</param>
    /// <param name="formatProvider">An <see cref="IFormatProvider"/> object that controls formatting.</param>
    public DebuggerWriter(int level, string category, IFormatProvider formatProvider)
        : base(formatProvider)
    {
        this.level = level;
        this.category = category;
        this.isOpen = true;
    }

    protected override void Dispose(bool disposing)
    {
        isOpen = false;
        base.Dispose(disposing);
    }

    public override void Write(char value)
    {
        if (!isOpen)
        {
            throw new ObjectDisposedException(null);
        }
#if !NETCORE
        Debugger.Log(level, category, value.ToString());
#endif
    }

    public override void Write(string value)
    {
        if (!isOpen)
        {
            throw new ObjectDisposedException(null);
        }
        if (value != null)
        {
#if !NETCORE
            Debugger.Log(level, category, value);
#endif
        }
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (!isOpen)
        {
            throw new ObjectDisposedException(null);
        }
        if (buffer == null || index < 0 || count < 0 || buffer.Length - index < count)
        {
            base.Write(buffer, index, count); // delegate throw exception to base class
        }
#if !NETCORE
        Debugger.Log(level, category, new string(buffer, index, count));
#endif
    }

    public override Encoding Encoding
    {
        get
        {
            if (encoding == null)
            {
                encoding = new UnicodeEncoding(false, false);
            }
            return encoding;
        }
    }

    public int Level
    {
        get { return level; }
    }

    public string Category
    {
        get { return category; }
    }
}
