Class ModSettingsSubmenu
    config(UI);

// Types
struct PlotIntSetting 
{
    var int Id;
    var int Value;
};
struct ModSettingItemData 
{
    var stringref srActionText;
    var string sActionText;
    var stringref srLeftText;
    var string sLeftText;
    var stringref srCenterText;
    var string sCenterText;
    var stringref srRightText;
    var string sRightText;
    var stringref srDescriptionTitleText;
    var string sDescriptionTitleText;
    var stringref srDescriptionText;
    var string sDescriptionText;
    var array<PlotIntSetting> ApplySettingInts;
    var array<int> ApplySettingBools;
    var int DisplayConditional;
    var int DisplayBool;
    var PlotIntSetting DisplayInt;
    var int EnableConditional;
    var int EnableBool;
    var PlotIntSetting EnableInt;
    var array<string> Images;
    var string SubMenuClassName;
    var Class<ModSettingsSubmenu> SubmenuClass;
    var ModSettingsSubmenu submenuInstance;
    var bool inlineSubmenu;
    var bool disabled;
    var bool hidden;
    var string comment;
    var array<string> displayVars;
    var array<string> displayRequiredPackageExports;
    var float sortPriority;
    
    structdefaultproperties
    {
        ApplySettingInts = ()
        Images = ()
        displayVars = ()
        displayRequiredPackageExports = ()
    }
};

// Variables
var config stringref srTitle;
var config string sTitle;
var config stringref srSubtitle;
var config string sSubtitle;
var config stringref defaultActionText;
var config array<ModSettingItemData> menuItems;
var transient int selectedIndex;
var transient int scrollIndex;
var transient array<string> inlineStack;

// Functions
public function bool OnRefreshMenu(Object outerMenu)
{
    return FALSE;
}
public function bool OnItemSelected(Object outerMenu, int selectionIndex)
{
    return FALSE;
}
public function bool OnActionButtonPressed(Object outerMenu, int selectionIndex)
{
    return FALSE;
}
public function bool OnAuxButtonPressed(Object outerMenu, int selectionIndex)
{
    return FALSE;
}
public function bool OnAux2ButtonPressed(Object outerMenu, int selectionIndex)
{
    return FALSE;
}
public function bool OnBackButtonPressed(Object outerMenu)
{
    return FALSE;
}

//class default properties can be edited in the Properties tab for the class's Default__ object.
defaultproperties
{
    srTitle = $200006
}