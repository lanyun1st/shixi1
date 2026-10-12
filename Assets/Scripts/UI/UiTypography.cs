using UnityEngine;
namespace Portfolio.UI
{
    public static class UiTypography
    {
        static Font body,heading;
        public static Font Body=>body!=null?body:body=Resources.Load<Font>("Fonts/SourceHanSansCN-Regular");
        public static Font Heading=>heading!=null?heading:heading=Resources.Load<Font>("Fonts/SourceHanSansCN-Bold");
    }
}
