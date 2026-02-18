using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Aero.DataStructures.Trees.Persistence.Indexes;

public interface IStringKey<TSelf> : IComparable<TSelf>
    where TSelf : unmanaged, IStringKey<TSelf>
{
    static abstract TSelf From(string value);
    string ToDisplayString();
    bool IsTruncated { get; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 65)]
public struct StringKey64 : IStringKey<StringKey64>
{
    private byte _data0, _data1, _data2, _data3, _data4, _data5, _data6, _data7;
    private byte _data8, _data9, _data10, _data11, _data12, _data13, _data14, _data15;
    private byte _data16, _data17, _data18, _data19, _data20, _data21, _data22, _data23;
    private byte _data24, _data25, _data26, _data27, _data28, _data29, _data30, _data31;
    private byte _data32, _data33, _data34, _data35, _data36, _data37, _data38, _data39;
    private byte _data40, _data41, _data42, _data43, _data44, _data45, _data46, _data47;
    private byte _data48, _data49, _data50, _data51, _data52, _data53, _data54, _data55;
    private byte _data56, _data57, _data58, _data59, _data60, _data61, _data62, _data63;
    private byte _isTruncated;

    public static StringKey64 From(string value)
    {
        var key = new StringKey64();
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));
        var encoded = Encoding.UTF8.GetBytes(value);
        var length = Math.Min(encoded.Length, 64);
        encoded.AsSpan(0, length).CopyTo(span);
        span.Slice(length).Clear();
        key._isTruncated = (byte)(encoded.Length > 64 ? 1 : 0);
        return key;
    }

    public bool IsTruncated => _isTruncated != 0;

    public int CompareTo(StringKey64 other)
    {
        var thisSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this, 1))[..64];
        var otherSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref other, 1))[..64];
        return thisSpan.SequenceCompareTo(otherSpan);
    }

    public string ToDisplayString()
    {
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this, 1))[..64];
        var end = span.IndexOf((byte)0);
        return Encoding.UTF8.GetString(end >= 0 ? span[..end] : span);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 33)]
public struct StringKey32 : IStringKey<StringKey32>
{
    private byte _data0, _data1, _data2, _data3, _data4, _data5, _data6, _data7;
    private byte _data8, _data9, _data10, _data11, _data12, _data13, _data14, _data15;
    private byte _data16, _data17, _data18, _data19, _data20, _data21, _data22, _data23;
    private byte _data24, _data25, _data26, _data27, _data28, _data29, _data30, _data31;
    private byte _isTruncated;

    public static StringKey32 From(string value)
    {
        var key = new StringKey32();
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));
        var encoded = Encoding.UTF8.GetBytes(value);
        var length = Math.Min(encoded.Length, 32);
        encoded.AsSpan(0, length).CopyTo(span);
        span.Slice(length).Clear();
        key._isTruncated = (byte)(encoded.Length > 32 ? 1 : 0);
        return key;
    }

    public bool IsTruncated => _isTruncated != 0;

    public int CompareTo(StringKey32 other)
    {
        var thisSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this, 1))[..32];
        var otherSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref other, 1))[..32];
        return thisSpan.SequenceCompareTo(otherSpan);
    }

    public string ToDisplayString()
    {
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this, 1))[..32];
        var end = span.IndexOf((byte)0);
        return Encoding.UTF8.GetString(end >= 0 ? span[..end] : span);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 129)]
public struct StringKey128 : IStringKey<StringKey128>
{
    private byte _data0, _data1, _data2, _data3, _data4, _data5, _data6, _data7;
    private byte _data8, _data9, _data10, _data11, _data12, _data13, _data14, _data15;
    private byte _data16, _data17, _data18, _data19, _data20, _data21, _data22, _data23;
    private byte _data24, _data25, _data26, _data27, _data28, _data29, _data30, _data31;
    private byte _data32, _data33, _data34, _data35, _data36, _data37, _data38, _data39;
    private byte _data40, _data41, _data42, _data43, _data44, _data45, _data46, _data47;
    private byte _data48, _data49, _data50, _data51, _data52, _data53, _data54, _data55;
    private byte _data56, _data57, _data58, _data59, _data60, _data61, _data62, _data63;
    private byte _data64, _data65, _data66, _data67, _data68, _data69, _data70, _data71;
    private byte _data72, _data73, _data74, _data75, _data76, _data77, _data78, _data79;
    private byte _data80, _data81, _data82, _data83, _data84, _data85, _data86, _data87;
    private byte _data88, _data89, _data90, _data91, _data92, _data93, _data94, _data95;
    private byte _data96, _data97, _data98, _data99, _data100, _data101, _data102, _data103;
    private byte _data104, _data105, _data106, _data107, _data108, _data109, _data110, _data111;
    private byte _data112, _data113, _data114, _data115, _data116, _data117, _data118, _data119;
    private byte _data120, _data121, _data122, _data123, _data124, _data125, _data126, _data127;
    private byte _isTruncated;

    public static StringKey128 From(string value)
    {
        var key = new StringKey128();
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));
        var encoded = Encoding.UTF8.GetBytes(value);
        var length = Math.Min(encoded.Length, 128);
        encoded.AsSpan(0, length).CopyTo(span);
        span.Slice(length).Clear();
        key._isTruncated = (byte)(encoded.Length > 128 ? 1 : 0);
        return key;
    }

    public bool IsTruncated => _isTruncated != 0;

    public int CompareTo(StringKey128 other)
    {
        var thisSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this, 1))[..128];
        var otherSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref other, 1))[..128];
        return thisSpan.SequenceCompareTo(otherSpan);
    }

    public string ToDisplayString()
    {
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this, 1))[..128];
        var end = span.IndexOf((byte)0);
        return Encoding.UTF8.GetString(end >= 0 ? span[..end] : span);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 257)]
public struct StringKey256 : IStringKey<StringKey256>
{
    private byte _data0, _data1, _data2, _data3, _data4, _data5, _data6, _data7;
    private byte _data8, _data9, _data10, _data11, _data12, _data13, _data14, _data15;
    private byte _data16, _data17, _data18, _data19, _data20, _data21, _data22, _data23;
    private byte _data24, _data25, _data26, _data27, _data28, _data29, _data30, _data31;
    private byte _data32, _data33, _data34, _data35, _data36, _data37, _data38, _data39;
    private byte _data40, _data41, _data42, _data43, _data44, _data45, _data46, _data47;
    private byte _data48, _data49, _data50, _data51, _data52, _data53, _data54, _data55;
    private byte _data56, _data57, _data58, _data59, _data60, _data61, _data62, _data63;
    private byte _data64, _data65, _data66, _data67, _data68, _data69, _data70, _data71;
    private byte _data72, _data73, _data74, _data75, _data76, _data77, _data78, _data79;
    private byte _data80, _data81, _data82, _data83, _data84, _data85, _data86, _data87;
    private byte _data88, _data89, _data90, _data91, _data92, _data93, _data94, _data95;
    private byte _data96, _data97, _data98, _data99, _data100, _data101, _data102, _data103;
    private byte _data104, _data105, _data106, _data107, _data108, _data109, _data110, _data111;
    private byte _data112, _data113, _data114, _data115, _data116, _data117, _data118, _data119;
    private byte _data120, _data121, _data122, _data123, _data124, _data125, _data126, _data127;
    private byte _data128, _data129, _data130, _data131, _data132, _data133, _data134, _data135;
    private byte _data136, _data137, _data138, _data139, _data140, _data141, _data142, _data143;
    private byte _data144, _data145, _data146, _data147, _data148, _data149, _data150, _data151;
    private byte _data152, _data153, _data154, _data155, _data156, _data157, _data158, _data159;
    private byte _data160, _data161, _data162, _data163, _data164, _data165, _data166, _data167;
    private byte _data168, _data169, _data170, _data171, _data172, _data173, _data174, _data175;
    private byte _data176, _data177, _data178, _data179, _data180, _data181, _data182, _data183;
    private byte _data184, _data185, _data186, _data187, _data188, _data189, _data190, _data191;
    private byte _data192, _data193, _data194, _data195, _data196, _data197, _data198, _data199;
    private byte _data200, _data201, _data202, _data203, _data204, _data205, _data206, _data207;
    private byte _data208, _data209, _data210, _data211, _data212, _data213, _data214, _data215;
    private byte _data216, _data217, _data218, _data219, _data220, _data221, _data222, _data223;
    private byte _data224, _data225, _data226, _data227, _data228, _data229, _data230, _data231;
    private byte _data232, _data233, _data234, _data235, _data236, _data237, _data238, _data239;
    private byte _data240, _data241, _data242, _data243, _data244, _data245, _data246, _data247;
    private byte _data248, _data249, _data250, _data251, _data252, _data253, _data254, _data255;
    private byte _isTruncated;

    public static StringKey256 From(string value)
    {
        var key = new StringKey256();
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));
        var encoded = Encoding.UTF8.GetBytes(value);
        var length = Math.Min(encoded.Length, 256);
        encoded.AsSpan(0, length).CopyTo(span);
        span.Slice(length).Clear();
        key._isTruncated = (byte)(encoded.Length > 256 ? 1 : 0);
        return key;
    }

    public bool IsTruncated => _isTruncated != 0;

    public int CompareTo(StringKey256 other)
    {
        var thisSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this, 1))[..256];
        var otherSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref other, 1))[..256];
        return thisSpan.SequenceCompareTo(otherSpan);
    }

    public string ToDisplayString()
    {
        var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this, 1))[..256];
        var end = span.IndexOf((byte)0);
        return Encoding.UTF8.GetString(end >= 0 ? span[..end] : span);
    }
}
