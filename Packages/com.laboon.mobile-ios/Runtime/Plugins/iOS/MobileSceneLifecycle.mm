#import <UIKit/UIKit.h>
#import "UnityAppController.h"
#import "MobileSceneLifecycle.h"

// Unity 2022.3 initializes its window in didFinishLaunching, before a scene exists.
// When the LaboonSceneLifecycle Info.plist key is true, defer that initialization until UIKit
// connects the app's single window scene. Without the key, Unity's normal path runs unchanged.
@interface UnityAppController (MobileSceneInitialization)
- (UIWindowScene *)pickStartupWindowScene:(NSSet<UIScene *> *)scenes;
@end

static BOOL MobileUsesSceneLifecycle(void)
{
    NSDictionary *info = NSBundle.mainBundle.infoDictionary;
    if (![info[@"LaboonSceneLifecycle"] boolValue])
        return NO;
    NSDictionary *manifest = info[@"UIApplicationSceneManifest"];
    NSArray *configurations = manifest[@"UISceneConfigurations"][@"UIWindowSceneSessionRoleApplication"];
    for (NSDictionary *configuration in configurations)
    {
        // Any delegate deriving from MobileSceneDelegate (including legacy aliases) is accepted.
        Class delegateClass = NSClassFromString(configuration[@"UISceneDelegateClassName"]);
        if (delegateClass != Nil && [delegateClass isSubclassOfClass:MobileSceneDelegate.class])
            return YES;
    }
    return NO;
}

@interface MobileAppController : UnityAppController
@property(nonatomic, copy) NSDictionary *pendingLaunchOptions;
@property(nonatomic, strong) UIWindowScene *connectingWindowScene;
- (void)connectWindowScene:(UIWindowScene *)scene;
@end

@implementation MobileAppController
- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)options
{
    // Preserve the normal Unity path when the marker is absent or a newer engine supplies its own scenes.
    if (!MobileUsesSceneLifecycle())
        return [super application:application didFinishLaunchingWithOptions:options];
    self.pendingLaunchOptions = options;
    return YES;
}

- (UIWindowScene *)pickStartupWindowScene:(NSSet<UIScene *> *)scenes
{
    if (self.connectingWindowScene != nil)
        return self.connectingWindowScene;
    if ([UnityAppController instancesRespondToSelector:@selector(pickStartupWindowScene:)])
        return [super pickStartupWindowScene:scenes];
    return nil;
}

- (void)connectWindowScene:(UIWindowScene *)scene
{
    self.connectingWindowScene = scene;
    if (self.window == nil)
        [super application:UIApplication.sharedApplication didFinishLaunchingWithOptions:self.pendingLaunchOptions];
    self.pendingLaunchOptions = nil;
    self.window.windowScene = scene;
    [self.window makeKeyAndVisible];
    self.connectingWindowScene = nil;
}
@end

IMPL_APP_CONTROLLER_SUBCLASS(MobileAppController)

@implementation MobileSceneDelegate
- (void)scene:(UIScene *)scene willConnectToSession:(UISceneSession *)session options:(UISceneConnectionOptions *)options
{
    if (![scene isKindOfClass:UIWindowScene.class])
        return;
    id delegate = UIApplication.sharedApplication.delegate;
    if (![delegate isKindOfClass:MobileAppController.class])
        return;
    MobileAppController *controller = (MobileAppController *)delegate;
    [controller connectWindowScene:(UIWindowScene *)scene];
    self.window = controller.window;
    [self scene:scene openURLContexts:options.URLContexts];
    for (NSUserActivity *activity in options.userActivities)
        [self scene:scene continueUserActivity:activity];
}

- (void)sceneWillEnterForeground:(UIScene *)scene
{
    [UIApplication.sharedApplication.delegate applicationWillEnterForeground:UIApplication.sharedApplication];
}

- (void)sceneDidBecomeActive:(UIScene *)scene
{
    id<UIApplicationDelegate> controller = UIApplication.sharedApplication.delegate;
    [controller applicationDidBecomeActive:UIApplication.sharedApplication];
    self.window = controller.window;
    self.window.windowScene = (UIWindowScene *)scene;
}

- (void)sceneWillResignActive:(UIScene *)scene
{
    [UIApplication.sharedApplication.delegate applicationWillResignActive:UIApplication.sharedApplication];
}

- (void)sceneDidEnterBackground:(UIScene *)scene
{
    [UIApplication.sharedApplication.delegate applicationDidEnterBackground:UIApplication.sharedApplication];
}

- (void)scene:(UIScene *)scene openURLContexts:(NSSet<UIOpenURLContext *> *)contexts
{
    for (UIOpenURLContext *context in contexts)
    {
        NSMutableDictionary *options = [NSMutableDictionary dictionary];
        if (context.options.sourceApplication)
            options[UIApplicationOpenURLOptionsSourceApplicationKey] = context.options.sourceApplication;
        if (context.options.annotation)
            options[UIApplicationOpenURLOptionsAnnotationKey] = context.options.annotation;
        options[UIApplicationOpenURLOptionsOpenInPlaceKey] = @(context.options.openInPlace);
        [UIApplication.sharedApplication.delegate application:UIApplication.sharedApplication openURL:context.URL options:options];
    }
}

- (void)scene:(UIScene *)scene continueUserActivity:(NSUserActivity *)activity
{
    [UIApplication.sharedApplication.delegate application:UIApplication.sharedApplication
        continueUserActivity:activity restorationHandler:^(NSArray *objects) {}];
}
@end
