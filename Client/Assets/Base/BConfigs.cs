using UnityEngine;
using UnityEditor;
public static class BConfigs{
    // APP ---------------------------------------------------------------------------
    public static string AndroidPath = Application.dataPath + "/../BuildTarget/Android/";
    public static string IOSPath = Application.dataPath + "/../BuildTarget/IOS/";
    public static string WindowsPath = Application.dataPath + "/../BuildTarget/Windows/";

    public static string BuildNameTxtPath = Application.dataPath + "/../buildname.txt";
    // AB包 -----------------------------------------------------------------------------
    // AB包目录路径
    public static string BunleTargetPath = Application.dataPath+"/../AssetBundle/" + EditorUserBuildSettings.activeBuildTarget.ToString();
    // ABConfig.asset的路径
    public static string ABConfigPath = "Assets/RealFram/Editor/Resource/ABConfig.asset";

    public static string AssetBundleConfigXmlPath = Application.dataPath + "/AssetbundleConfig.xml";
    // 配置表 -----------------------------------------------------------------------------
    //打包时生成AB包配置表的二进制路径
    public static string ABBytePath = " Assets/RealFram/Data/ABData/AssetBundleConfig.bytes";
    //xml文件夹路径
    public static string XmlPath = "Assets/RealFram/Data/Xml/";
    //二进制文件夹路径
    public static string BinaryPath = "Assets/RealFram/Data/Binary/";
    // Excel 文件路径
    public static string ExcelPath = Application.dataPath + "/../Data/Excel/";
    // Reg 文件路径
    public static string m_RegPath = Application.dataPath + "/../Data/Reg/";
    // 测试数据
    public static string TestReadXmlPath = Application.dataPath + "/../Data/Reg/MonsterData.xml";
    public static string TestWriteExcel = Application.dataPath + "/../Data/Excel/G怪物.xlsx";
}