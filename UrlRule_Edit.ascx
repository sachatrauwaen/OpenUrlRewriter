<%@ Control language="C#" Inherits="DotNetNuke.Modules.OpenUrlRewriter.UrlRule_Edit" AutoEventWireup="false"  Codebehind="UrlRule_Edit.ascx.cs" %>
<%@ Register TagPrefix="dnn" TagName="label" Src="~/controls/LabelControl.ascx" %>
<%@ Register TagPrefix="dnn" Assembly="DotNetNuke" Namespace="DotNetNuke.UI.WebControls" %>
<%@ Register TagPrefix="dnn" Assembly="DotNetNuke.Web" Namespace="DotNetNuke.Web.UI.WebControls" %>


<div class="dnnForm" id="form-demo">
    <asp:Label ID="Label1" runat="server" CssClass="dnnFormMessage dnnFormInfo" ResourceKey="Intro"  />
    <asp:HyperLink ID="HyperLink2" runat="server" ImageUrl="~/images/help.gif" NavigateUrl="https://openurlrewriter.codeplex.com/wikipage?title=Custom%20rules" Target="_blank"  ></asp:HyperLink>
    <asp:HyperLink ID="HyperLink1" runat="server" NavigateUrl="https://openurlrewriter.codeplex.com/wikipage?title=Custom%20rules" Target="_blank" Text="More info about custom rules" ></asp:HyperLink>
    <div class="dnnFormItem dnnFormHelp dnnClear">
        <p class="dnnFormRequired"><asp:Label ID="Label2" runat="server" ResourceKey="Required Indicator" /></p>
    </div>
    <fieldset>
         <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="ChoiceDropDown" ResourceKey="RuleType" />
            <asp:DropDownList runat="server" ID="ddlRuleType"  >               
                <asp:ListItem Text="Culture" Value="0" Enabled="false" />
                <asp:ListItem Text="Tab" Value="1" Enabled="false" />
                <asp:ListItem Text="Module" Value="2" Enabled="false" />
                <asp:ListItem Text="Custom" Value="3" />
            </asp:DropDownList>
        </div>    
        
         <div class="dnnFormItem" style="display:none">
            <dnn:Label runat="server" ControlName="ChoiceDropDown" ResourceKey="CultureCode" />
            <asp:DropDownList runat="server" ID="ddlCultureCode" Enabled="false">               
                
            </asp:DropDownList>
        </div>    
        
        <div class="dnnFormItem" >
            <dnn:Label runat="server" ControlName="ChoiceDropDown" ResourceKey="Tab" />
            <asp:DropDownList runat="server" ID="ddlTab" DataTextField="IndentedTabName" DataValueField="TabID" AppendDataBoundItems="true">
                <asp:ListItem Value="-1" Text="-- All --"></asp:ListItem>               
                
            </asp:DropDownList>
        </div>
        
        <div class="dnnFormItem" >
            <dnn:Label runat="server" ControlName="tbParameters" ResourceKey="Parameters" />
            <asp:TextBox runat="server"  ID="tbParameters"  />
        </div>
        
        <div class="dnnFormItem" style="display:none">
            <dnn:Label runat="server" ControlName="cbRemoveTab" ResourceKey="RemoveTab" />
            <asp:CheckBox runat="server" ID="cbRemoveTab" Enabled="false"/>
        </div>    
        
        <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="ddlAction" ResourceKey="Action" />
            <asp:DropDownList runat="server" ID="ddlAction" >               
                <asp:ListItem Text="Rewrite" Value="0" />
                <asp:ListItem Text="Redirect" Value="1" />
            </asp:DropDownList>
        </div>
    
        <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="tbUrl" ResourceKey="Url" />
            <asp:TextBox runat="server"  ID="tbUrl"  />
        </div>    
        
        <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="tbRedirectDestination" ResourceKey="RedirectDestination" />
            <asp:TextBox runat="server" ID="tbRedirectDestination"  />
        </div>    
        
        <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="ddlRedirectStatus" ResourceKey="RedirectStatus" />
            <asp:DropDownList runat="server" ID="ddlRedirectStatus" > 
                <asp:ListItem Text="Aucun" Value="0"  />              
                <asp:ListItem Text="301 Permanent" Value="301"  />
                <asp:ListItem Text="302 Temporary" Value="302" />
                <asp:ListItem Text="404 Not found" Value="404" />
            </asp:DropDownList>
        </div>
    
        
        
    </fieldset>
    <ul class="dnnActions dnnClear">
        <li><asp:LinkButton ID="lbSave" runat="server" CssClass="dnnPrimaryAction" 
                ResourceKey="Save" onclick="lbSave_Click" /></li>
        <li><asp:HyperLink ID="hlCancel" runat="server" CssClass="dnnSecondaryAction" NavigateUrl="/" ResourceKey="Cancel" /></li>
        <li><asp:LinkButton ID="lbDelete" runat="server" CssClass="dnnSecondaryAction" 
                ResourceKey="Delete" onclick="lbDelete_Click" /></li>
    </ul>
</div>
            
