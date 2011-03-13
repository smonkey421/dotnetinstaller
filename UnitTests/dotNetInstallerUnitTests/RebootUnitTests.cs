using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using InstallerLib;
using dotNetUnitTestsRunner;
using System.IO;
using Microsoft.Win32;

namespace dotNetInstallerUnitTests
{
    [TestFixture]
    public class RebootUnitTests
    {
        [Test]
        public void TestRebootRequired()
        {
            Console.WriteLine("TestRebootRequired");

            // a configuration with a component that requires reboot, exit code will be 3010
            ConfigFile configFile = new ConfigFile();
            SetupConfiguration setupConfiguration = new SetupConfiguration();
            configFile.Children.Add(setupConfiguration);
            ComponentCmd cmd = new ComponentCmd();
            cmd.command = "cmd.exe /C exit /b 3010";
            cmd.required_install = true;
            cmd.returncodes_reboot = "3010";
            setupConfiguration.Children.Add(cmd);
            string configFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xml");
            Console.WriteLine("Writing '{0}'", configFilename);
            configFile.SaveAs(configFilename);
            Assert.AreEqual(3010, dotNetInstallerExeUtils.Run(configFilename));
            File.Delete(configFilename);
            dotNetInstallerExeUtils.DisableRunOnReboot();
        }

        [Test]
        public void TestRebootExitCode()
        {
            Console.WriteLine("TestRebootExitCode");

            // reboot exit code doesn't override a previous error
            ConfigFile configFile = new ConfigFile();
            SetupConfiguration setupConfiguration = new SetupConfiguration();
            configFile.Children.Add(setupConfiguration);
            ComponentCmd cmd_error = new ComponentCmd();
            cmd_error.command = "cmd.exe /C exit /b 42";
            cmd_error.required_install = true;
            cmd_error.allow_continue_on_error = true;
            cmd_error.default_continue_on_error = true;
            setupConfiguration.Children.Add(cmd_error);
            ComponentCmd cmd_reboot = new ComponentCmd();
            cmd_reboot.command = "cmd.exe /C exit /b 3010";
            cmd_error.required_install = true;
            cmd_reboot.returncodes_reboot = "3010";
            setupConfiguration.Children.Add(cmd_reboot);
            string configFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xml");
            Console.WriteLine("Writing '{0}'", configFilename);
            configFile.SaveAs(configFilename);
            Assert.AreEqual(42, dotNetInstallerExeUtils.Run(configFilename));
            File.Delete(configFilename);
            dotNetInstallerExeUtils.DisableRunOnReboot();
        }

        [Test]
        public void TestNoRunOnReboot()
        {
            Console.WriteLine("TestNoRunOnReboot");

            //return reboot code 3010
            //simulate passing /noRunOnReboot to dotNetInstaller on command line
            //ensure RunOnReboot registry key is not written

            ConfigFile configFile = new ConfigFile();
            SetupConfiguration setupConfiguration = new SetupConfiguration();
            configFile.Children.Add(setupConfiguration);
            ComponentCmd cmd_reboot = new ComponentCmd();
            cmd_reboot.command = "cmd.exe /C exit /b 3010";
            cmd_reboot.returncodes_reboot = "3010";
            setupConfiguration.Children.Add(cmd_reboot);

            string configFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xml");
            Console.WriteLine("Writing '{0}'", configFilename);
            configFile.SaveAs(configFilename);

            dotNetInstallerExeUtils.RunOptions options = new dotNetInstallerExeUtils.RunOptions(configFilename);
            options.noRunOnReboot = true;

            Assert.AreEqual(3010, dotNetInstallerExeUtils.Run(options));

            File.Delete(configFilename);

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
                {
                    Assert.AreEqual(null, key.GetValue(Path.GetFileName(dotNetInstallerExeUtils.Executable)));
                }
            }
            catch
            {
                //remove RunOnReboot registry value if AssertionException is thrown
                dotNetInstallerExeUtils.DisableRunOnReboot();
                throw;
            }
        }
    }
}
