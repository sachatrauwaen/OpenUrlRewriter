<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="UrlLog_Settings.ascx.cs" Inherits="Satrabel.Modules.OpenUrlRewriter.UrlLog_Settings" %>
<%@ Register TagPrefix="dnn" TagName="label" Src="~/controls/LabelControl.ascx" %>
<%@ Register TagPrefix="dnn" Assembly="DotNetNuke" Namespace="DotNetNuke.UI.WebControls" %>
<%@ Register TagPrefix="dnn" Assembly="DotNetNuke.Web" Namespace="DotNetNuke.Web.UI.WebControls" %>


<div class="dnnForm" id="form-demo">
    <asp:Label ID="Label1" runat="server" CssClass="dnnFormMessage dnnFormInfo" ResourceKey="Intro"  />
    <div class="dnnFormItem dnnFormHelp dnnClear">
        <p class="dnnFormRequired"><asp:Label ID="Label2" runat="server" ResourceKey="Required Indicator" /></p>
    </div>
    <fieldset>
    
         <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="cbLogEnabled" ResourceKey="LogEnabled" />
            <asp:CheckBox runat="server" ID="cbLogEnabled" />
        </div>
        
        <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="AgreeCheckbox" ResourceKey="LogAuthentificatedUsers" />
            <asp:CheckBox runat="server" ID="cbLogAuthentificatedUsers" />
        </div>
        
        <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="cbLogEachUrlOneTime" ResourceKey="LogEachUrlOneTime" />
            <asp:CheckBox runat="server" ID="cbLogEachUrlOneTime" />
        </div>
        
        <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="cbLogStatusCode200" ResourceKey="LogStatusCode200" />
            <asp:CheckBox runat="server" ID="cbLogStatusCode200" />
        </div>
        
        
    </fieldset>
    <ul class="dnnActions dnnClear">
        <li><asp:LinkButton ID="lbSave" runat="server" CssClass="dnnPrimaryAction" 
                ResourceKey="Save" onclick="lbSave_Click" /></li>
        <li><asp:HyperLink ID="hlCancel" runat="server" CssClass="dnnSecondaryAction" NavigateUrl="/" ResourceKey="Cancel" /></li>
    </ul>
</div>
            
