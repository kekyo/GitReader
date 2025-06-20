////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

// Imported from (MIT licensed):
// https://github.com/microsoft/referencesource/blob/master/mscorlib/system/io/streamreader.cs

// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==

using GitReader.Internal;
using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;

#nullable disable

namespace GitReader.IO;

// This class is a modified version of StreamReader for GitReader.
// Intended to improve performance of asynchronous I/O.
// Therefore, features not directly related have been removed from the original implementation.
// .NET Core family of codes are complicated to modify because they use Span<T>.
// Therefore, the modified code based on .NET Framework.

// In the original code, it was implemented to persistently truncate asynchronous contexts with `ConfigureAwait(false)`.
// But when making very chatty calls, the creation of the `ConfiguredAwaitable()` object has proven to be costly.
// So we have removed these calls.
// GitReader will incur the cost of asynchronous context restoration if invoked while maintaining an asynchronous context,
// but we recommend that forign callers detach before invoking the exposed methods themselves.

internal sealed class AsyncTextReader
{
    // Using a 1K byte buffer and a 4K FileStream buffer works out pretty well
    // perf-wise.  On even a 40 MB text file, any perf loss by using a 4K
    // buffer is negated by the win of allocating a smaller byte[], which 
    // saves construction time.  This does break adaptive buffering,
    // but this is slightly faster.
    private const int DefaultBufferSize = 1024;

    private Stream stream;
    private Encoding encoding;
    private Decoder decoder;
    private byte[] byteBuffer;
    private char[] charBuffer;
    private byte[] _preamble;   // Encoding's preamble, which identifies this encoding.
    private int charPos;
    private int charLen;
    // Record the number of valid bytes in the byteBuffer, for a few checks.
    private int byteLen;
    // This is used only for preamble detection
    private int bytePos;

    // This is the maximum number of chars we can get from one call to 
    // ReadBuffer.  Used so ReadBuffer can tell when to copy data into
    // a user's char[] directly, instead of our internal char[].
    private int _maxCharsPerBuffer;

    // We will support looking for byte order marks in the stream and trying
    // to decide what the encoding might be from the byte order marks, IF they
    // exist.  But that's all we'll do.  
    private bool _detectEncoding;

    // Whether we must still check for the encoding's given preamble at the
    // beginning of this file.
    private bool _checkPreamble;

    // SimpleReader by default will ignore illegal UTF8 characters. We don't want to 
    // throw here because we want to be able to read ill-formed data without choking. 
    // The high level goal is to be tolerant of encoding errors when we read and very strict 
    // when we write. Hence, default StreamWriter encoding will throw on error.   

    public AsyncTextReader(Stream stream)
    {
        this.stream = stream;
        this.encoding = Utilities.UTF8;
        decoder = encoding.GetDecoder();
        byteBuffer = new byte[DefaultBufferSize];
        _maxCharsPerBuffer = encoding.GetMaxCharCount(DefaultBufferSize);
        charBuffer = new char[_maxCharsPerBuffer];
        byteLen = 0;
        bytePos = 0;
        _detectEncoding = true;
        _preamble = encoding.GetPreamble();
        _checkPreamble = (_preamble.Length > 0);
    }

    // Trims n bytes from the front of the buffer.
    private void CompressBuffer(int n)
    {
        Debug.Assert(byteLen >= n, "CompressBuffer was called with a number of bytes greater than the current buffer length.  Are two threads using this SimpleReader at the same time?");
        Buffer.BlockCopy(byteBuffer, n, byteBuffer, 0, byteLen - n);
        byteLen -= n;
    }

    private void DetectEncoding()
    {
        if (byteLen < 2)
            return;
        _detectEncoding = false;
        bool changedEncoding = false;
        if (byteBuffer[0] == 0xFE && byteBuffer[1] == 0xFF)
        {
            // Big Endian Unicode

            encoding = new UnicodeEncoding(true, true);
            CompressBuffer(2);
            changedEncoding = true;
        }

        else if (byteBuffer[0] == 0xFF && byteBuffer[1] == 0xFE)
        {
            // Little Endian Unicode, or possibly little endian UTF32
            if (byteLen < 4 || byteBuffer[2] != 0 || byteBuffer[3] != 0)
            {
                encoding = new UnicodeEncoding(false, true);
                CompressBuffer(2);
                changedEncoding = true;
            }
#if FEATURE_UTF32   
            else {
                encoding = new UTF32Encoding(false, true);
                CompressBuffer(4);
            changedEncoding = true;
        }
#endif            
        }

        else if (byteLen >= 3 && byteBuffer[0] == 0xEF && byteBuffer[1] == 0xBB && byteBuffer[2] == 0xBF)
        {
            // UTF-8
            encoding = Utilities.UTF8;
            CompressBuffer(3);
            changedEncoding = true;
        }
#if FEATURE_UTF32            
        else if (byteLen >= 4 && byteBuffer[0] == 0 && byteBuffer[1] == 0 &&
                 byteBuffer[2] == 0xFE && byteBuffer[3] == 0xFF) {
            // Big Endian UTF32
            encoding = new UTF32Encoding(true, true);
            CompressBuffer(4);
            changedEncoding = true;
        }
#endif            
        else if (byteLen == 2)
            _detectEncoding = true;
        // Note: in the future, if we change this algorithm significantly,
        // we can support checking for the preamble of the given encoding.

        if (changedEncoding)
        {
            decoder = encoding.GetDecoder();
            _maxCharsPerBuffer = encoding.GetMaxCharCount(byteBuffer.Length);
            charBuffer = new char[_maxCharsPerBuffer];
        }
    }

    // Trims the preamble bytes from the byteBuffer. This routine can be called multiple times
    // and we will buffer the bytes read until the preamble is matched or we determine that
    // there is no match. If there is no match, every byte read previously will be available 
    // for further consumption. If there is a match, we will compress the buffer for the 
    // leading preamble bytes
    private bool IsPreamble()
    {
        if (!_checkPreamble)
            return _checkPreamble;

        Debug.Assert(bytePos <= _preamble.Length, "_compressPreamble was called with the current bytePos greater than the preamble buffer length.  Are two threads using this SimpleReader at the same time?");
        int len = (byteLen >= (_preamble.Length)) ? (_preamble.Length - bytePos) : (byteLen - bytePos);

        for (int i = 0; i < len; i++, bytePos++)
        {
            if (byteBuffer[bytePos] != _preamble[bytePos])
            {
                bytePos = 0;
                _checkPreamble = false;
                break;
            }
        }

        Debug.Assert(bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this SimpleReader at the same time?");

        if (_checkPreamble)
        {
            if (bytePos == _preamble.Length)
            {
                // We have a match
                CompressBuffer(_preamble.Length);
                bytePos = 0;
                _checkPreamble = false;
                _detectEncoding = false;
            }
        }

        return _checkPreamble;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public async ValueTask<String> ReadLineAsync(CancellationToken ct)
#else
    public async Task<String> ReadLineAsync(CancellationToken ct)
#endif
    {
        if (CharPos_Prop == CharLen_Prop && (await ReadBufferAsync(ct) == 0))
            return null;

        StringBuilder sb = null;

        do
        {
            char[] tmpCharBuffer = CharBuffer_Prop;
            int tmpCharLen = CharLen_Prop;
            int tmpCharPos = CharPos_Prop;
            int i = tmpCharPos;

            do
            {
                char ch = tmpCharBuffer[i];

                // Note the following common line feed chars:
                // \n - UNIX   \r\n - DOS   \r - Mac
                if (ch == '\r' || ch == '\n')
                {
                    String s;

                    if (sb != null)
                    {
                        sb.Append(tmpCharBuffer, tmpCharPos, i - tmpCharPos);
                        s = sb.ToString();
                    }
                    else
                    {
                        s = new String(tmpCharBuffer, tmpCharPos, i - tmpCharPos);
                    }

                    CharPos_Prop = tmpCharPos = i + 1;

                    if (ch == '\r' && (tmpCharPos < tmpCharLen || (await ReadBufferAsync(ct)) > 0))
                    {
                        tmpCharPos = CharPos_Prop;
                        if (CharBuffer_Prop[tmpCharPos] == '\n')
                            CharPos_Prop = ++tmpCharPos;
                    }

                    return s;
                }

                i++;

            } while (i < tmpCharLen);

            i = tmpCharLen - tmpCharPos;
            if (sb == null) sb = new StringBuilder(i + 80);
            sb.Append(tmpCharBuffer, tmpCharPos, i);

        } while (await ReadBufferAsync(ct) > 0);

        return sb.ToString();
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public async ValueTask<String> ReadToEndAsync(CancellationToken ct)
#else
    public async Task<String> ReadToEndAsync(CancellationToken ct)
#endif
    {
        // Call ReadBuffer, then pull data out of charBuffer.
        StringBuilder sb = new StringBuilder(CharLen_Prop - CharPos_Prop);
        do
        {
            int tmpCharPos = CharPos_Prop;
            sb.Append(CharBuffer_Prop, tmpCharPos, CharLen_Prop - tmpCharPos);
            CharPos_Prop = CharLen_Prop;  // We consumed these characters
            await ReadBufferAsync(ct);
        } while (CharLen_Prop > 0);

        return sb.ToString();
    }

    #region Private properties for async method performance
    // Access to instance fields of MarshalByRefObject-derived types requires special JIT helpers that check
    // if the instance operated on is remote. This is optimised for fields on this but if a method is Async
    // and is thus lifted to a state machine type, access will be slow.
    // As a workaround, we either cache instance fields in locals or use properties to access such fields.

    // See Dev11 bug #370300 for more info.
    
    private Int32 CharLen_Prop {
        get { return charLen; }
        set { charLen = value; }
    }

    private Int32 CharPos_Prop {
        get { return charPos; }
        set { charPos = value; }
    }

    private Int32 ByteLen_Prop {
        get { return byteLen; }
        set { byteLen = value; }
    }

    private Int32 BytePos_Prop {
        get { return bytePos; }
        set { bytePos = value; }
    }

    private Byte[] Preamble_Prop {
        get { return _preamble; }
    }

    private bool CheckPreamble_Prop {
        get { return _checkPreamble; }
    }

    private Decoder Decoder_Prop {
        get { return decoder; }
    }

    private bool DetectEncoding_Prop {
        get { return _detectEncoding; }
    }

    private Char[]  CharBuffer_Prop {
        get { return charBuffer; }
    }

    private Byte[]  ByteBuffer_Prop {
        get { return byteBuffer; }
    }

    private Stream Stream_Prop {
        get { return stream; }
    }

    private Int32 MaxCharsPerBuffer_Prop {
        get { return _maxCharsPerBuffer; }
    }
    #endregion Private properties for async method performance

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    private async ValueTask<int> ReadBufferAsync(CancellationToken ct)
#else
    private async Task<int> ReadBufferAsync(CancellationToken ct)
#endif
    {
        CharLen_Prop = 0;
        CharPos_Prop = 0;
        Byte[] tmpByteBuffer = ByteBuffer_Prop;
        Stream tmpStream = Stream_Prop;
        
        if (!CheckPreamble_Prop)
            ByteLen_Prop = 0;
        do {
            if (CheckPreamble_Prop) {
                Debug.Assert(BytePos_Prop <= Preamble_Prop.Length, "possible bug in _compressPreamble. Are two threads using this SimpleReader at the same time?");
                int tmpBytePos = BytePos_Prop;
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
                int len = tmpStream is IValueTaskStream vts ?
                    await vts.ReadValueTaskAsync(tmpByteBuffer, tmpBytePos, tmpByteBuffer.Length - tmpBytePos, ct) :
                    await tmpStream.ReadAsync(tmpByteBuffer, tmpBytePos, tmpByteBuffer.Length - tmpBytePos, ct);
#else
                int len = await tmpStream.ReadAsync(tmpByteBuffer, tmpBytePos, tmpByteBuffer.Length - tmpBytePos, ct);
#endif
                Debug.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");
                
                if (len == 0) {
                    // EOF but we might have buffered bytes from previous 
                    // attempt to detect preamble that needs to be decoded now
                    if (ByteLen_Prop > 0)
                    {
                        CharLen_Prop += Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, CharBuffer_Prop, CharLen_Prop);
                        // Need to zero out the _byteLen after we consume these bytes so that we don't keep infinitely hitting this code path
                        BytePos_Prop = 0; ByteLen_Prop = 0;
                    }
                    
                    return CharLen_Prop;
                }
                
                ByteLen_Prop += len;
            }
            else {
                Debug.Assert(BytePos_Prop == 0, "_bytePos can be non zero only when we are trying to _checkPreamble. Are two threads using this SimpleReader at the same time?");
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
                ByteLen_Prop = tmpStream is IValueTaskStream vts ?
                    await vts.ReadValueTaskAsync(tmpByteBuffer, 0, tmpByteBuffer.Length, ct) :
                    await tmpStream.ReadAsync(tmpByteBuffer, 0, tmpByteBuffer.Length, ct);
#else
                ByteLen_Prop = await tmpStream.ReadAsync(tmpByteBuffer, 0, tmpByteBuffer.Length, ct);
#endif
                Debug.Assert(ByteLen_Prop >= 0, "Stream.Read returned a negative number!  Bug in stream class.");
                
                if (ByteLen_Prop == 0)  // We're at EOF
                    return CharLen_Prop;
            }
            
            // Check for preamble before detect encoding. This is not to override the
            // user suppplied Encoding for the one we implicitly detect. The user could
            // customize the encoding which we will loose, such as ThrowOnError on UTF8
            if (IsPreamble()) 
                continue;

            // If we're supposed to detect the encoding and haven't done so yet,
            // do it.  Note this may need to be called more than once.
            if (DetectEncoding_Prop && ByteLen_Prop >= 2)
                DetectEncoding();

            CharLen_Prop += Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, CharBuffer_Prop, CharLen_Prop);
        } while (CharLen_Prop == 0);
        
        return CharLen_Prop;
    }
}
