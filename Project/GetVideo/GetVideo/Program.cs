// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;


#region 年龄分级获取模块=》grade

Console.WriteLine(@"请输入年龄分级:0\13\18");

string GetGrade()
{
    return Console.ReadLine() switch
    {
        "0" => "Everyone",
        "13" => "Questionable",
        "18" => "Mature",
        _ => GetGrade()
    };
}

var grade = GetGrade();

#endregion

Console.WriteLine(
    @"steam资源文件夹(盘符开始,steamapps结尾,默认'D:\Steam\steamapps')"); //* 自用输入路径"F:\\SteamLibrary\\steamapps\\workshop\\content\\431960"
var steamShop = new DirectoryInfo((Console.ReadLine() ?? "D:\\Steam\\steamapps") + "\\workshop\\content\\431960");
Console.WriteLine(
    @"输出文件夹(自动新建文件夹'\Video',默认'Desktop\Video')"); //* 自用输出路径"C:\\Users\\wenkuo\\Desktop\\GetVideo\\Video\\"
var outPath = Directory
    .CreateDirectory((Console.ReadLine() ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)) +
                     "\\Video\\").FullName;


var jsonBuff = new byte[1024];
Console.WriteLine(steamShop.GetDirectories()[0].Name);
foreach (var _directoryInfo in steamShop.GetDirectories())
{
    //? 判断是否存在json文件
    var jsonInfo = _directoryInfo.GetFiles("*.json").Length != 0
        ? _directoryInfo.GetFiles("*.json")[0]
        : null;
    if (jsonInfo==null) continue;
    
    var jsonContentInfo = JsonSerializer.Deserialize<JsonNeedInfo>(jsonInfo.OpenRead());
    jsonInfo.OpenRead().Read(jsonBuff);

    if (jsonContentInfo?.contentrating==grade)
        if (_directoryInfo.GetFiles("*.mp4").Length != 0)
        {
            byte[] videoBuff;
            var videoStream = _directoryInfo.GetFiles("*.mp4")[0].OpenRead();

            string videoName = ConvertToEn(jsonContentInfo.title);
            Console.WriteLine(videoName);
            string outVideoPath = outPath + videoName + ".mp4";
            if(File.Exists(outPath + videoName+ ".mp4"))
            {
                Console.WriteLine("\n检测到重命名\n");
                var i=0;
                while (File.Exists(outPath + videoName+i+ ".mp4"))
                {
                    i += 1;
                }
                outVideoPath = outPath + videoName + i + ".mp4";
            }//? 命名重复性检测

            var videoWriter = new BinaryWriter(new FileStream(outVideoPath, FileMode.Create));
            try
            {
                videoBuff = new byte[videoStream.Length];
                videoStream.Read(videoBuff);

                Console.WriteLine(_directoryInfo.GetFiles("*.mp4")[0].Name + "\n大小:" + videoStream.Length +"\n生成路径:"+ outPath + videoName + ".mp4"+ "\n");
                videoWriter.Write(videoBuff);
            }
            catch (OverflowException e) //? 超出长度
            {
                Console.WriteLine("超大杯:" + _directoryInfo.GetFiles("*.mp4")[0].Name + "\n大小:" +
                                  videoStream.Length+"\n生成路径:"+ outPath + videoName + ".mp4"+"\n");

                //? 计算分次批数
                var byteArrayNum = videoStream.Length % 2147483591 == 0
                    ? (int)(videoStream.Length / 2147483591)
                    : (int)(videoStream.Length / 2147483591) + 1;

                var nextIndex = 0;

                for (var i = 0; i < byteArrayNum; i++)
                {
                    videoStream.Seek(nextIndex, SeekOrigin.Begin);
                    if (i == byteArrayNum - 1) //? 最后一段
                    {
                        var surplusLength = videoStream.Length - videoStream.Position;
                        videoBuff = new byte[surplusLength];
                        videoStream.Read(videoBuff);
                    }
                    else
                    {
                        videoBuff = new byte[2147483591];
                        videoStream.Read(videoBuff, 0, videoBuff.Length);
                    }

                    videoWriter.Seek(nextIndex, SeekOrigin.Begin);
                    videoWriter.Write(videoBuff);
                    nextIndex += videoBuff.Length;
                }
            }

            videoWriter.Dispose();
        }
}

string ConvertToEn(string text)
{
    const string no = "\\/:*?\"<>|";
    char[] c = text.ToCharArray();
    for (int i = 0; i < c.Length; i++)
    {
        int n = no.IndexOf(c[i]);
        if (n != -1)
        {
            List<char> cList = c.ToList();
            cList.Remove(c[i]);
            c = cList.ToArray();
        }
    }
    return new string(c);
}
class JsonNeedInfo
{
    public string contentrating { get; set; }
    public string title { get; set; }
}