using Sunny.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace modbus
{
    /// <summary>
    /// INI 读写辅助类：封装与本地 Setup.ini 的交互。
    /// 当前固定读写 Section "Setup" 下的 X / Y / U 三个键，
    /// 并向上层暴露统一的保存 / 加载方法。
    /// </summary>
    internal class iniHelper
    {
        // INI 文件路径，默认指向程序当前目录下的 Setup.ini
        private readonly IniFile _ini = new IniFile(Directory.GetCurrentDirectory() + "Setup.ini");

        // Section / Key 常量，集中维护，便于修改
        private const string Section = "Setup";
        private const string KeyX = "X";
        private const string KeyY = "Y";
        private const string KeyU = "U";

        /// <summary>
        /// 保存 X / Y / U 三个文本框的值到 Setup.ini。
        /// </summary>
        public void WriteLocal(UITextBox xBox, UITextBox yBox, UITextBox uBox)
        {
            _ini.Write(Section, KeyX, xBox.Text);
            _ini.Write(Section, KeyY, yBox.Text);
            _ini.Write(Section, KeyU, uBox.Text);
            _ini.UpdateFile();
        }

        /// <summary>
        /// 从 Setup.ini 加载 X / Y / U 并写回传入的文本框。
        /// 读取失败时使用空字符串作为默认值。
        /// </summary>
        public void LoadLocal(UITextBox xBox, UITextBox yBox, UITextBox uBox)
        {
            xBox.Text = _ini.ReadString(Section, KeyX, "");
            yBox.Text = _ini.ReadString(Section, KeyY, "");
            uBox.Text = _ini.ReadString(Section, KeyU, "");
        }
    }
}