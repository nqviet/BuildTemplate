#import <UIKit/UIKit.h>

// Laboon Mobile iOS scene lifecycle. Subclass MobileSceneDelegate to keep an old
// UISceneDelegateClassName working during migration (see the package sample).
@interface MobileSceneDelegate : UIResponder <UIWindowSceneDelegate>
@property(nonatomic, strong) UIWindow *window;
@end
