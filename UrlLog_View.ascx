<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="UrlLog_View.ascx.cs" Inherits="Satrabel.Modules.OpenUrlRewriter.UrlLog_View" %>

<%@ Register TagPrefix="dnn" Namespace="DotNetNuke.Web.Client.ClientResourceManagement" Assembly="DotNetNuke.Web.Client" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>

<div class="dnnForm" >

    <fieldset>
        <div class="dnnFormItem">    
            <dnn:label ID="Label1" runat="server" Text="Filter" />
            <asp:TextBox ID="tbFilter" runat="server"></asp:TextBox>
        </div>
    </fieldset>
<asp:GridView ID="gvLogs" runat="server" BorderStyle="None" GridLines="None" CssClass="dnnGrid" EnableViewState="false" AutoGenerateColumns="false"
                Width="100%">
               
     <headerstyle CssClass="dnnGridHeader" HorizontalAlign="Left" />
     <rowstyle CssClass="dnnGridItem" />
     <alternatingrowstyle CssClass="dnnGridAltItem" /> 
               

<Columns>
    <asp:BoundField DataField="DateTime" HeaderText="Date" ItemStyle-Width="50"/>
    <asp:BoundField DataField="StatusCode" HeaderText="Status"/>
    <asp:BoundField DataField="UserId" HeaderText="UserId"/>
    <asp:BoundField DataField="TabId" HeaderText="TabId"/>
    
    <asp:TemplateField HeaderText="Url">
        <ItemTemplate>
            <b>Referrer: </b><asp:Label ID="Label1" runat="server" Text='<%# Eval("Referrer")%>' ></asp:Label><br />
            <b>Rewrite : </b><asp:Label ID="Label2" runat="server" Text='<%# Eval("URL")%>' ></asp:Label><br />
            <b>Original : </b><asp:Label ID="Label3" runat="server" Text='<%# Eval("OriginalURL")%>' ></asp:Label><br />
            <b>Redirect: </b><asp:Label ID="Label4" runat="server" Text='<%# Eval("RedirectURL")%>' ></asp:Label><br />            
        </ItemTemplate>
    </asp:TemplateField>
    <asp:TemplateField ItemStyle-Width="150" HeaderText="UserHost">
        <ItemTemplate>
            <asp:Label ID="Label1" runat="server" Text='<%# Eval("UserHostAddress")%>' ></asp:Label><br />
            <asp:Label ID="Label2" runat="server" Text='<%# Eval("UserHostName")%>' ></asp:Label><br />
        </ItemTemplate>
    </asp:TemplateField>
    <asp:TemplateField HeaderText="UserAgent" ItemStyle-Width="150">
        <ItemTemplate>
            <asp:Label ID="Label1" runat="server" Text='<%# Eval("UserAgent").ToString().Length > 50 ? Eval("UserAgent").ToString().Substring(0, 50)+"..." :  Eval("UserAgent")  %>' ToolTip='<%# Eval("UserAgent") %>'></asp:Label><br />
            
        </ItemTemplate>
    </asp:TemplateField>
    

</Columns>

<EmptyDataTemplate>
    No logs
</EmptyDataTemplate>
</asp:GridView>
</div>

<dnn:DnnJsInclude runat="server" FilePath="~/DesktopModules/OpenUrlRewriter/js/jquery.uitablefilter.js"  />

<script type="text/jscript">

    var gvLogs = $('#<%= gvLogs.ClientID %>')

    $('#<%= tbFilter.ClientID %>').keyup(function() {
        $.uiTableFilter( gvLogs, this.value );
    })

</script>