<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="TestHtml_View.ascx.cs" Inherits="Satrabel.Modules.OpenUrlRewriter.TestHtml_View" %>
<%@ Register TagPrefix="dnn" Namespace="DotNetNuke.Web.Client.ClientResourceManagement" Assembly="DotNetNuke.Web.Client" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>
<div class="dnnForm" >

    <fieldset>
        <div class="dnnFormItem">    
            <dnn:label ID="Label2" runat="server" Text="Table Name" />
            <asp:TextBox ID="tbTableName" runat="server" Text="HtmlText" ></asp:TextBox>
        </div>
        <div class="dnnFormItem">    
            <dnn:label ID="Label3" runat="server" Text="Primary Key Field" />
            <asp:TextBox ID="tbPrimaryKeyField" runat="server" Text="ItemID"></asp:TextBox>
        </div>
        <div class="dnnFormItem">    
            <dnn:label ID="Label4" runat="server" Text="Html Field" />
            <asp:TextBox ID="tbHtmlField" runat="server" Text="Content"></asp:TextBox>
        </div>
        <div class="dnnFormItem">    
            <dnn:label ID="Label5" runat="server" Text="Base Url" />
            <asp:TextBox ID="tbBaseUrl" runat="server"></asp:TextBox>
        </div>
         <div class="dnnFormItem">
            <dnn:Label ID="Label6" runat="server" ControlName="cbDecodeEncode" Text="Encode/Decode Html" />
            <asp:CheckBox runat="server" ID="cbDecodeEncode" Checked="true" />
        </div>



    </fieldset>

    <ul class="dnnActions dnnClear">
        <li><asp:LinkButton ID="lbTest" runat="server" CssClass="dnnPrimaryAction" Text="Search"
                onclick="lbTest_Click"  /></li>
        <li><asp:LinkButton ID="lbReplace" runat="server" CssClass="dnnPrimaryAction" 
                Text="Search & Replace" onclick="lbReplace_Click"
                 /></li>
        
    </ul>

     <fieldset>
        <div class="dnnFormItem">    
            <dnn:label ID="Label1" runat="server" Text="Filter" />
            <asp:TextBox ID="tbFilter" runat="server"></asp:TextBox>
        </div>




    </fieldset>

<asp:GridView ID="GridView1" runat="server" BorderStyle="None" GridLines="None" CssClass="dnnGrid" 
                EnableViewState="false"  Width="100%" Visible="true">         

     <headerstyle CssClass="dnnGridHeader" HorizontalAlign="Left" />
     <rowstyle CssClass="dnnGridItem" />
     <alternatingrowstyle CssClass="dnnGridAltItem" /> 
     
     
     <Columns>
     
    
    
    </Columns>
    

</asp:GridView>

</div>
<dnn:DnnJsInclude runat="server" FilePath="~/DesktopModules/OpenUrlRewriter/js/jquery.uitablefilter.js"  />

<script type="text/jscript">

    var GridView1 = $('#<%= GridView1.ClientID %>')

    $('#<%= tbFilter.ClientID %>').keyup(function() {
        $.uiTableFilter( GridView1, this.value );
    })

</script>