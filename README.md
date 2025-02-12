# Unity3D-MOBA-Character-Controller


![demo](\ReadmeFiles\demo.gif)

This is a basic 3D character controller based on NavMesh pathfinding, which can be used for Moba games and RTS games.

The included functions are: right-click on the floor to move, click on the floor when releasing skills to wait for the skill to be released before executing the move. Press S to stop the action immediately.

This project does not include camera control, you need to implement it yourself. The animation model resources used are all from Unity's official StarterAssets, you can replace your own models and animations. You can easily replace your own click floor effects too.

The UnitController.cs has reserved skill call interfaces (normal attacks are also regarded as skills).

### How To Use In Your Own Project

1. Import Package

2. Add NavMesh to your ground and bake;

   ![image-20250212173154594](\ReadmeFiles\image-20250212173154594.png)

3. Set the ground's layer the same as  UnitController and ClickVisual

![image-20250212172547189](\ReadmeFiles\image-20250212172547189.png)

![image-20250212172717622](\ReadmeFiles\image-20250212172717622.png)

![image-20250212172749947](\ReadmeFiles\image-20250212172749947.png)

4. Now you can play！




---

### 中文说明

这是一个基础的基于NavMesh寻路的3D角色控制器，可以用于Moba游戏和RTS游戏。

已经包含的功能：右键点击地板移动，释放技能时点击地板会等待技能释放结束后再执行移动。按S立即停止行动。

这个项目不包含相机控制，你需要自己实现。使用的动画模型资源均来自于Unity官方的StarterAssets，你可以替换自己的模型，动画。UnitController中留有技能调用的接口（普通攻击也视作技能）。
