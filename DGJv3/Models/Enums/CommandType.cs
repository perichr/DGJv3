using System.ComponentModel;

namespace DGJv3
{
    public enum CommandType
    {
        [Description("无效命令")]
        Null,
        [Description("点歌")]
        Add,
        [Description("取消点歌")]
        Cancel,
        [Description("重新点上一首歌曲")]
        AddLast,
        [Description("再次点当前歌曲")]
        AddCurent,
        [Description("返回歌曲平台和ID")]
        Info,
        [Description("下一首（结束当前歌曲并播放下一首）")]
        Next,
        [Description("切歌（结束当前歌曲并播放之后指定序号的歌曲）")]
        Skip,
        [Description("暂停")]
        Pause,
        [Description("播放")]
        Play,
        [Description("调整音量")]
        Volume
    }
}
