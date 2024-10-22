using FTOptix.OPCUAClient;
using FTOptix.OPCUAServer;
﻿namespace Luxlib.Robot
{
    public static class OmnicoreNodes
    {
        public static string Https() { return "https://"; }
        public static string IOsystemNS() { return "/rw/iosystem/signals"; }
        public static string Mastership() { return "/rw/mastership"; }
        public static string Rapid() { return "/rw/rapid/symbol/RAPID"; }
        public static string ActionMastershipRequest() { return "/edit/request"; }
        public static string ActionMastershipRelease() { return "/edit/release"; }
        public static string ActionSignalSet() { return "/set-value"; }
        public static string WriteVarValue(string input) { return $"value={input}"; }
        public static string WriteSignalValue(bool input) { return $"lvalue={(input == true ? "1" : "0")}"; }
        public static string FileServiceHome() { return "/fileservice/$home"; }
        public static string RapidModuleText(string task, string module) { return $@"/rw/rapid/tasks/{task}/modules/{module.Replace(".modx", "")}/text"; }
        public static string RapidModuleSave(string task, string module) { return $@"/rw/rapid/tasks/{task}/modules/{module.Replace(".modx", "")}/save"; }
        public static string ModuleNameAndPath(string name, string path) { return $@"name={name}&path={path}"; }
        public static string WriteAnalogSignalValue(string input) { return $"lvalue={input}"; }
        public static string WriteGroupSignalValue(string input) { return $"lvalue={input}"; }
        public static string ImplicitMasteship() { return "?mastership=implicit"; }
        public static string WriteVarValueWithImplicitMastership(string input) { return $"value={input}" + ImplicitMasteship(); }
        public static string Subscription() { return "/subscription"; }
    }
}
