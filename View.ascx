<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="View.ascx.cs" Inherits="Satrabel.Modules.OpenUrlRewriter.View" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>



<div class="" style="">
<asp:Label ID="lStatus" runat="server" CssClass="dnnFormMessage dnnFormSuccess" EnableViewState="false" Visible="false" Width="50%"></asp:Label>
</div>

<div class="clear"></div>

<div  id="Line1" >

        
        <div Class="OpenUrlRewriterPanel dnnLeft" style="width:30%;">
            <div class="dnnForm" style="min-width:200px;">
                        
                <h2><asp:Label ID="Label1" runat="server" Text="Url rules cache" ></asp:Label></h2>
                <div class="grids">
                    <asp:GridView ID="gvCache" runat="server" BorderStyle="None" GridLines="None" CssClass="dnnGrid" EnableViewState="false">
                        <HeaderStyle HorizontalAlign="Left" />
                        <EmptyDataTemplate>
                            No rules in cache
                        </EmptyDataTemplate>
                    </asp:GridView>

                    <asp:GridView ID="gvClashes" runat="server" BorderStyle="None" GridLines="None" CssClass="dnnGrid" EnableViewState="false">
                        <EmptyDataTemplate>
                            No duplicate urls
                        </EmptyDataTemplate>                
                    </asp:GridView>
                </div>
                
                <ul class="dnnActions dnnClear">
                    <li><asp:LinkButton ID="lbViewCache" runat="server" CssClass="dnnPrimaryAction" Text="View Cache" /></li>    
                    
                    <li><asp:LinkButton ID="LinkButton1" runat="server" CssClass="dnnSecondaryAction" Text="Clear cache" ToolTip="Clear cache" onclick="ClearCache_Click" /></li>
                </ul>   
                
                
            </div>
        </div>              

        <div Class="OpenUrlRewriterPanel dnnLeft" style="width:24%;">
            <div class="dnnForm" style="min-width:200px;">
                        
                <h2><asp:Label ID="Label3" runat="server" Text="Stored url rules" ></asp:Label></h2>
                <div class="grids">
                <asp:GridView ID="gvRules" runat="server" BorderStyle="None" GridLines="None" CssClass="dnnGrid" EnableViewState="false">
                    <HeaderStyle HorizontalAlign="Left" />
                    <EmptyDataTemplate>
                        No rules
                        
                    </EmptyDataTemplate>
                </asp:GridView>
                </div>
                <ul class="dnnActions dnnClear">
                    <li><asp:LinkButton ID="lbViewRules" runat="server" CssClass="dnnPrimaryAction" Text="View Rules"  /></li>    
                    <li><asp:LinkButton ID="lbAddRule" runat="server" CssClass="dnnSecondaryAction" Text="Add" ToolTip="Add custom rule" /></li>          
                    <li><asp:LinkButton ID="lbClearRules" runat="server" CssClass="dnnSecondaryAction" Text="Clear" ToolTip="Clear rules (non custom)" OnClick="ClearRules_Click"  /></li>
                </ul>   
                
                
            </div>
        </div>
        
          <div Class="OpenUrlRewriterPanel dnnLeft" style="width:24%">
            <div class="dnnForm" style="min-width:200px;">
                        
                <h2><asp:Label ID="Label2" runat="server" Text="Url logs" ></asp:Label></h2>
                <div class="grids">
                <asp:GridView ID="gvUrlLogs" runat="server" BorderStyle="None" GridLines="None" CssClass="dnnGrid" EnableViewState="false">
                    <HeaderStyle HorizontalAlign="Left" />
                    <EmptyDataTemplate>
                        No logs
                    </EmptyDataTemplate>
                </asp:GridView>
                 <asp:GridView ID="gvDuplicates" runat="server" BorderStyle="None" GridLines="None" CssClass="dnnGrid" EnableViewState="false">
                    <EmptyDataTemplate>
                        No duplicates
                    </EmptyDataTemplate>
                </asp:GridView>
                
                </div>
                <ul class="dnnActions dnnClear">
                    <li><asp:LinkButton ID="lbViewLog" runat="server" CssClass="dnnPrimaryAction" Text="View logs" /></li>    

                    <li><asp:LinkButton ID="lbClearLogs" runat="server" CssClass="dnnSecondaryAction" Text="Clear" ToolTip="Clear all logs" OnClick="ClearLogs_Click"  /></li>                        
                </ul>   
            </div>
        </div>

        <div class="clear"></div>
</div>

<div  id="Line2" >
    
    <div Class="OpenUrlRewriterPanel dnnLeft" style="width:92%;">
         <h2><asp:Label ID="Label4" runat="server" Text="Site settings" ></asp:Label></h2>
    
      <div class="dnnForm" id="tabs-demo">
        <ul class="dnnAdminTabNav">
            
            <li><a href="#Providers">Providers</a></li>
            <li><a href="#PageMetaData">Page meta</a></li>
            <li><a href="#Logging">Logging</a></li>
            <li><a href="#W3C">W3C</a></li>
            <li><a href="#404NotFound">404 Not Found</a></li>
            <li><a href="#Components">Components</a></li>
            <li><a href="#Portals">Portals</a></li>
            
        </ul>
        
          <div id="Providers" style="min-width:200px;padding-top:10px;">
            
            <div class="grids">
            
                <asp:GridView ID="gvProviders" runat="server" BorderStyle="None" GridLines="None" CssClass="dnnGrid" EnableViewState="false" 
                                OnRowDataBound="gvProviders_RowDataBound" AutoGenerateColumns="false">
                    <HeaderStyle HorizontalAlign="Left" />
                    <Columns>
                        <asp:TemplateField>
                            <ItemTemplate>
                                <asp:HyperLink ID="HyperLink1" runat="server" ImageUrl="~/images/help.gif" NavigateUrl='<%# Eval("HelpUrl") %>' Target="_blank" Visible='<%# Eval("HelpUrl") != null %>' ></asp:HyperLink>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="Name" HeaderText="Name" />
                        <asp:TemplateField HeaderText="Rules" >                           
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Enabled" >    
                            <ItemTemplate>
                                <asp:CheckBox ID="cbEnabled" runat="server" />
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Settings">
                            <ItemTemplate >
                                <asp:CheckBoxList ID="cblSettings" runat="server" RepeatLayout="Flow" RepeatDirection="Horizontal">

                                </asp:CheckBoxList>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        No providers
                    </EmptyDataTemplate>
                </asp:GridView>
               
                
            </div>
           
        </div>
    
        <div id="PageMetaData" style="min-width:200px;">
            
            <asp:Label ID="Label5" runat="server" CssClass="dnnFormMessage dnnFormInfo" ResourceKey="PageMetaIntro"  />
            <div >
                <fieldset>
                    <div class="dnnFormItem">
                        <dnn:Label runat="server" ControlName="cbDisableSiteIndex" ResourceKey="DisableSiteIndex"  />
                        <asp:CheckBox runat="server" ID="cbDisableSiteIndex" />
                    </div>
                    <div class="dnnFormItem">
                        <dnn:Label runat="server" ControlName="cbDisableTermsIndex" ResourceKey="DisableTermsIndex"  />
                        <asp:CheckBox runat="server" ID="cbDisableTermsIndex" />
                    </div>
                    <div class="dnnFormItem">
                        <dnn:Label runat="server" ControlName="cbDisablePivacyIndex" ResourceKey="DisablePivacyIndex"  />
                        <asp:CheckBox runat="server" ID="cbDisablePivacyIndex" />
                    </div>
                </fieldset>
                
            </div>
          
        </div>
        
         <div id="Logging" style="min-width:200px;">
            
            <asp:Label ID="Label7" runat="server" CssClass="dnnFormMessage dnnFormInfo" ResourceKey="Intro"  />
            <div >
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
            </div>
                                  
        </div>        
        <div id="W3C" style="min-width:200px;">            
            <asp:Label ID="Label9" runat="server" CssClass="dnnFormMessage dnnFormInfo" ResourceKey="W3CIntro"  />
            <div >
                <fieldset>
                
                    <div class="dnnFormItem">
                        <dnn:Label ID="Label8" runat="server" ControlName="cbW3C" ResourceKey="W3C"  />
                        <asp:CheckBox runat="server" ID="cbW3C" />
                    </div>    
                </fieldset>                               
            </div>                                  
        </div>
        <div id="404NotFound" style="min-width:200px;">
            <asp:Label ID="Label15" runat="server" CssClass="dnnFormMessage dnnFormInfo" ResourceKey="404Intro"  />            
            <div >
                <fieldset>
                
                    <div class="dnnFormItem">
                        <dnn:Label ID="Label14" runat="server" ControlName="cbEnhanced404" ResourceKey="Enhanced404"  />
                        <asp:CheckBox runat="server" ID="cbEnhanced404" />
                    </div>    

                    <div class="dnnFormItem" >
                        <dnn:Label ID="Label13" runat="server" ControlName="ddlTab" ResourceKey="404Tab" />
                        <asp:DropDownList runat="server" ID="ddlTab" DataTextField="IndentedTabName" DataValueField="TabID" AppendDataBoundItems="true">
                            <asp:ListItem Value="-1" Text="-- select --"></asp:ListItem>               
                
                        </asp:DropDownList>
                    </div>

                </fieldset>                               
            </div>
                                  
        </div>
        
         <div id="Components" style="min-width:200px;">
            <div  >
                 <fieldset>
                <asp:Label ID="Label6" runat="server" CssClass="dnnFormMessage dnnFormInfo" Text="web.config check"  />            
                </fieldset>
                <fieldset>                
                    <asp:Label ID="lFriendlyUrlProvider" runat="server" Text="FriendlyUrl provider" />
                    <asp:Label ID="lUrlRewriter" runat="server" Text="Url Rewriter" />               
                    <asp:Label ID="lSitemapHandler" runat="server" Text="Sitemap handler" />
                    
                    <asp:Label ID="lSitemapProvider" runat="server" Text="Sitemap provider" />                
                    <asp:Label ID="lCachingProvider" runat="server" Text="Caching provider" Visible="false" />
                </fieldset>
            </div>
            <div >
                <fieldset>
                
                    
                     <div class="dnnFormItem">                        
                        <dnn:Label runat="server" ControlName="cbSitemapProvider" ResourceKey="SitemapProvider" Visible="false" />
                        <asp:CheckBox runat="server" ID="cbSitemapProvider" Visible="false" />
                    </div>
                    
                                                  
                </fieldset>                               
            </div>
                                  
        </div>
        
        <div id="Portals" style="min-width:200px;padding-top:10px;">
            
            <div class="grids">
                
                <asp:GridView ID="gvPortals" runat="server" BorderStyle="None" GridLines="None" CssClass="dnnGrid" EnableViewState="false" 
                                OnRowDataBound="gvPortals_RowDataBound" AutoGenerateColumns="false">
                    <HeaderStyle HorizontalAlign="Left" />
                    <Columns>
                        <asp:BoundField DataField="PortalName" HeaderText="PortalName" />
                        <asp:TemplateField HeaderText="Rules" >                           
                        </asp:TemplateField>
                         <asp:TemplateField HeaderText="Memory" Visible="false" >                           
                        </asp:TemplateField>                                                                   
                    </Columns>
                    <EmptyDataTemplate>
                        No Portals
                    </EmptyDataTemplate>
                </asp:GridView>
               
                <asp:LinkButton ID="lbShowPortals" runat="server" onclick="lbShowPortals_Click">Show portals stats</asp:LinkButton>
            </div>
           
        </div>
      
      
        <ul class="dnnActions dnnClear">
                <li><asp:LinkButton ID="lbSaveMeta" runat="server" CssClass="dnnPrimaryAction" Text="Update settings" OnClick="SaveMeta_Click" /></li>    
                
        </ul>    
      </div>  
    </div>

    <div class="clear"></div>
</div>    



<div  id="Line3" >
    
    <div Class="OpenUrlRewriterPanel dnnLeft" style="width:92%;">
         <h2><asp:Label ID="Label10" runat="server" Text="Test, Search & Replace" ></asp:Label></h2>
         <div id="Div1" style="min-width:200px;">
            
            <asp:Label ID="Label11" runat="server" CssClass="dnnFormMessage dnnFormInfo" ResourceKey="TestIntro"  />
            <div >
                <fieldset>
                    <div class="dnnFormItem">
                        <dnn:Label ID="Label12" runat="server" ControlName="tbUrl" Text="Url" />
                        <asp:TextBox runat="server" ID="tbUrl" />                        
                        <dnn:Label ID="lUrlResult" runat="server" />
                    </div>
                </fieldset>
                
            </div>
            <ul class="dnnActions dnnClear">
                <li><asp:LinkButton ID="lbTestUrl" runat="server" CssClass="dnnPrimaryAction" Text="Test Url" onclick="lbTestUrl_Click"  /></li>                    
                <li><asp:LinkButton ID="lbTestHtml" runat="server" CssClass="dnnSecondaryAction" Text="Search & Replace in Html ..."   /></li>  
            </ul>    
        </div>

    </div>
</div>

<asp:Label ID="lTrace" runat="server" ></asp:Label>

<script type="text/javascript">
    
        var gridheight = 0;
        $("#Line1 .grids").each(function () {
            gridheight = Math.max( gridheight, $(this).height());
        });                  
        if (gridheight > 0) {        
        $('#Line1 .grids').height(gridheight);
        }
        
        var gridheight = 0;
        $("#Line1 .dnnActions").each(function () {
            gridheight = Math.max( gridheight, $(this).height());
        });                  
        if (gridheight > 0) {        
        $('#Line1 .dnnActions').height(gridheight);
        }
        
         jQuery(function ($) {
        $('#tabs-demo').dnnTabs();
    });
    
      
    
</script>            
 		               