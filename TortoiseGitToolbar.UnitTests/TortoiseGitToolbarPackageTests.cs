﻿using System;
using System.ComponentModel.Design;
using System.Reflection;
using FizzWare.NBuilder;
using MattDavies.TortoiseGitToolbar;
using MattDavies.TortoiseGitToolbar.Config.Constants;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VsSDK.UnitTestLibrary;
using NUnit.Framework;
using TortoiseGitToolbar.UnitTests.Helpers;

namespace TortoiseGitToolbar.UnitTests
{
    [TestFixture]
    public class TortoiseGitToolbarPackageShould
    {
        private IVsPackage _package;
        private MethodInfo _getServiceMethod;
        private OleServiceProvider _serviceProvider;

        [SetUp]
        public void Setup()
        {
            _package = new TortoiseGitToolbarPackage();
            _serviceProvider = OleServiceProvider.CreateOleServiceProviderWithBasicServices();
            _getServiceMethod = typeof(Package).GetMethod("GetService", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Test]
        public void Implement_vspackage()
        {
            Assert.That(_package, Is.Not.Null, "The package does not implement IVsPackage");
        }

        [Test]
        public void Correctly_set_site()
        {
            Assert.That(_package.SetSite(_serviceProvider), Is.EqualTo(0), "Package SetSite did not return S_OK");
        }

        private readonly CommandId[] _commands = EnumHelper.GetValues<CommandId>();
        [TestCaseSource("_commands")]
        public void Ensure_all_tortoisegit_commands_exist(CommandId commandId)
        {
            var command = GetMenuCommand(commandId);
            
            Assert.That(command, Is.Not.Null, string.Format("Couldn't find command for {0}", commandId));
        }

        [TestCase(CommandId.CmdCommit, "Commit")]
        [TestCase(CommandId.CmdResolve, "Resolve")]
        [TestCase(CommandId.CmdPull, "Pull")]
        [TestCase(CommandId.CmdPush, "Push")]
        [TestCase(CommandId.CmdLog, "Log")]
        [TestCase(CommandId.CmdBash, "Bash")]
        public void Ensure_all_tortoisegit_commands_bind_to_correct_event_handlers(CommandId commandId, string handlerName)
        {
            var command = GetMenuCommand(commandId);

            var execHandler = typeof(MenuCommand).GetField("execHandler", BindingFlags.NonPublic | BindingFlags.Instance);
            
            Assert.That(execHandler, Is.Not.Null);
            Assert.That(((EventHandler) execHandler.GetValue(command)).Method.Name, Is.EqualTo(handlerName));
        }

        [TestCase("Commit")]
        [TestCase("Resolve")]
        [TestCase("Pull")]
        [TestCase("Push")]
        [TestCase("Log")]
        [TestCase("Bash")]
        public void Invoke_all_command_handlers_without_exception(string commandHandlerName)
        {
            try
            {
                _package.SetSite(_serviceProvider);

                var uishellMock = UIShellServiceMock.GetUiShellInstance();
                _serviceProvider.AddService(typeof (SVsUIShell), uishellMock, true);
                var commandHandler = _package.GetType().GetMethod(commandHandlerName, BindingFlags.Instance | BindingFlags.NonPublic);
                
                Assert.That(commandHandler, Is.Not.Null, string.Format("Failed to get the private method {0}", commandHandlerName));
                Assert.DoesNotThrow(() => commandHandler.Invoke(_package, new object[] {null, null}));
            }
            finally
            {
                _serviceProvider.RemoveService(typeof(SVsUIShell));
            }
        }

        private MenuCommand GetMenuCommand(CommandId commandId)
        {
            _package.SetSite(_serviceProvider);

            var menuCommandID = new CommandID(PackageConstants.guidTortoiseGitToolbarCmdSet, (int)commandId);
            var menuCommandService = _getServiceMethod.Invoke(_package, new object[] { (typeof(IMenuCommandService)) }) as OleMenuCommandService;
            return menuCommandService != null ? menuCommandService.FindCommand(menuCommandID) : null;
        }
    }
}
