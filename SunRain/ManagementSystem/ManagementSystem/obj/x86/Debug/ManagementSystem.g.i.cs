﻿#pragma checksum "..\..\..\ManagementSystem.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "9E22B17F2F5C6A8C1B8A3CE515345A9A"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.296
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace ManagementSystem {
    
    
    /// <summary>
    /// MainWindow
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
    public partial class MainWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 173 "..\..\..\ManagementSystem.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ProgressBar pbarStatus;
        
        #line default
        #line hidden
        
        
        #line 185 "..\..\..\ManagementSystem.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TreeView tvTerminal;
        
        #line default
        #line hidden
        
        
        #line 187 "..\..\..\ManagementSystem.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TabControl tcTerminal;
        
        #line default
        #line hidden
        
        
        #line 195 "..\..\..\ManagementSystem.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.DataGrid dgLog;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/ManagementSystem;component/managementsystem.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\ManagementSystem.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 5 "..\..\..\ManagementSystem.xaml"
            ((ManagementSystem.MainWindow)(target)).Loaded += new System.Windows.RoutedEventHandler(this.Window_Load);
            
            #line default
            #line hidden
            return;
            case 2:
            
            #line 31 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.ClearLog_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 3:
            
            #line 36 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.SaveLog_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            
            #line 42 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.Window_Exit);
            
            #line default
            #line hidden
            return;
            case 5:
            
            #line 50 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.ServerConfig_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            
            #line 56 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.LocalConfig_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            
            #line 63 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.AddDTU_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 8:
            
            #line 75 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.DeleteDTU_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 9:
            
            #line 83 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.MenuItem)(target)).Click += new System.Windows.RoutedEventHandler(this.About_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 10:
            
            #line 106 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Reconnect_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 11:
            
            #line 113 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.ServerConfig_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 12:
            
            #line 119 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.LocalConfig_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 13:
            
            #line 126 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.AddDTU_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 14:
            
            #line 138 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.DeleteDTU_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 15:
            
            #line 145 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.ClearLog_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 16:
            
            #line 151 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.SaveLog_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 17:
            
            #line 158 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.About_MenuItem_Click);
            
            #line default
            #line hidden
            return;
            case 18:
            
            #line 165 "..\..\..\ManagementSystem.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Window_Exit);
            
            #line default
            #line hidden
            return;
            case 19:
            this.pbarStatus = ((System.Windows.Controls.ProgressBar)(target));
            return;
            case 20:
            this.tvTerminal = ((System.Windows.Controls.TreeView)(target));
            return;
            case 21:
            this.tcTerminal = ((System.Windows.Controls.TabControl)(target));
            return;
            case 22:
            this.dgLog = ((System.Windows.Controls.DataGrid)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}
