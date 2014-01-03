<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="UrlRuleCache_View.ascx.cs" Inherits="Satrabel.Modules.OpenUrlRewriter.UrlRuleCache_View" %>
<%@ Register TagPrefix="dnn" Namespace="DotNetNuke.Web.Client.ClientResourceManagement" Assembly="DotNetNuke.Web.Client" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>
<div class="dnnForm" >

    <fieldset>
        <div class="dnnFormItem">    
            <dnn:label ID="Label1" runat="server" Text="Filter" />
            <asp:TextBox ID="tbFilter" runat="server"></asp:TextBox>
        </div>
    </fieldset>

<asp:GridView ID="GridView1" runat="server" BorderStyle="None" GridLines="None" CssClass="dnnGrid" 
                EnableViewState="false"  Width="100%" Visible="true" AutoGenerateColumns="false">         

     <headerstyle CssClass="dnnGridHeader" HorizontalAlign="Left" />
     <rowstyle CssClass="dnnGridItem" />
     <alternatingrowstyle CssClass="dnnGridAltItem" /> 
     
     
     <Columns>
     
        <asp:BoundField DataField="RuleTypeString" HeaderText="Type" SortExpression="RuleTypeString" >
            <HeaderStyle HorizontalAlign="Left" />
        </asp:BoundField>
        
        <asp:BoundField DataField="CultureCode" HeaderText="Culture" SortExpression="CultureCode" >
            <HeaderStyle HorizontalAlign="Left" />
        </asp:BoundField>
        
        <asp:BoundField DataField="TabId" HeaderText="TabId" SortExpression="TabId" >
            <HeaderStyle HorizontalAlign="Left" />
        </asp:BoundField>
        
        <asp:BoundField DataField="Parameters" HeaderText="Parameters" SortExpression="Parameters" >
            <HeaderStyle HorizontalAlign="Left" />
        </asp:BoundField>
        
         <asp:CheckBoxField DataField="RemoveTab" HeaderText="NoTab" SortExpression="RemoveTab" >
            <HeaderStyle HorizontalAlign="Left" />
        </asp:CheckBoxField>
        
       
         <asp:BoundField DataField="ActionString" HeaderText="Action" SortExpression="RuleActionString" >
            <HeaderStyle HorizontalAlign="Left" />
        </asp:BoundField>
        
        <asp:BoundField DataField="Url" HeaderText="Url" SortExpression="Url" >
            <HeaderStyle HorizontalAlign="Left" />
        </asp:BoundField>
        
        <asp:BoundField DataField="RedirectDestination" HeaderText="Destination" SortExpression="RedirectDestination" >
            <HeaderStyle HorizontalAlign="Left" />
        </asp:BoundField>
        
        <asp:BoundField DataField="RedirectStatus" HeaderText="Status" SortExpression="RedirectStatus" >
            <HeaderStyle HorizontalAlign="Left" />
        </asp:BoundField>

    
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