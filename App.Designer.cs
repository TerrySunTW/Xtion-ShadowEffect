﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ShadowEffect {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "12.0.0.0")]
    internal sealed partial class App : global::System.Configuration.ApplicationSettingsBase {
        
        private static App defaultInstance = ((App)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new App())));
        
        public static App Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int ShadowQuantity {
            get {
                return ((int)(this["ShadowQuantity"]));
            }
            set {
                this["ShadowQuantity"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int ShadowSpeed {
            get {
                return ((int)(this["ShadowSpeed"]));
            }
            set {
                this["ShadowSpeed"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.1")]
        public float ShadowAlpha {
            get {
                return ((float)(this["ShadowAlpha"]));
            }
            set {
                this["ShadowAlpha"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.1")]
        public float ShadowBlurRadio {
            get {
                return ((float)(this["ShadowBlurRadio"]));
            }
            set {
                this["ShadowBlurRadio"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsReverse {
            get {
                return ((bool)(this["IsReverse"]));
            }
            set {
                this["IsReverse"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsBlackShadow {
            get {
                return ((bool)(this["IsBlackShadow"]));
            }
            set {
                this["IsBlackShadow"] = value;
            }
        }
    }
}
