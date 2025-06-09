using System;
using Unity.Collections;

namespace UnityFileDebugLogger
{
	public interface IFileDebugLogger<FixedStringType> where FixedStringType :
        unmanaged
        , INativeList<byte>
        , IUTF8Bytes
        , IComparable<String>
        , IEquatable<String>
        , IComparable<FixedString32Bytes>
        , IEquatable<FixedString32Bytes>
        , IComparable<FixedString64Bytes>
        , IEquatable<FixedString64Bytes>
        , IComparable<FixedString128Bytes>
        , IEquatable<FixedString128Bytes>
        , IComparable<FixedString512Bytes>
        , IEquatable<FixedString512Bytes>
        , IComparable<FixedString4096Bytes>
        , IEquatable<FixedString4096Bytes>
    {
    }

}
