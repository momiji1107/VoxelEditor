# Voxel Editor

A Unity Editor Tool for easily placing voxel-style Prefabs on a 3D grid, similar to a 3D Tilemap.

## Features

- Place voxel-style Prefabs on a 3D grid.
- Erase placed Prefabs.
- Pen mode and Eraser mode.
- Drag placement and drag erasing.
- Prefab rotation on the X, Y, and Z axes.
- Grid display ON/OFF.
- Adjustable Grid Size.
- Prefab thumbnail selection.
- Multiple VoxelPrefabDatabase support.
- Switch between multiple Prefab databases from the Editor GUI.
- Automatically organize placed Prefabs under the VoxelWorld GameObject.
- Undo/Redo support through the Unity Editor.
- Prefabs can be registered in a ScriptableObject-based database.

---

# Installation

## Method 1: Install from a Unity Package

1. Download the Voxel Editor package.

2. Open the Unity project where you want to install the package.

3. Select:

   `Window > Package Manager`

4. Click the `+` button in the upper-left corner of the Package Manager window.

5. Select:

   `Install package from disk...`

6. Select the `package.json` file included in the Voxel Editor package.

7. Unity will import the package automatically.

After installation, the Voxel Editor Tool will be available in the Unity Editor.

---

## Method 2: Install by Importing the Package

If the package is provided as a `.unitypackage` file:

1. Open the Unity project.

2. Select:

   `Assets > Import Package > Custom Package...`

3. Select the Voxel Editor `.unitypackage` file.

4. Confirm the files that you want to import.

5. Click Import.

The Voxel Editor will be imported into the project.

---

## Method 3: Install from a Git URL

You can install Voxel Editor directly from a Git repository using the `Unity Package Manager`.

### Requirements

Git must be installed on your computer.

If the repository uses Git LFS, Git LFS must also be installed.

### Installation Steps

1. Open the Unity project where you want to install Voxel Editor.

2. Open:

   `Window > Package Manager`

3. Click the `+` button in the upper-left corner of the Package Manager window.

4. Select:

   `Add package from git URL`

5. Enter the following Git URL:

   `https://github.com/momiji1107/VoxelEditor.git?path=/Assets/VoxelEditor`

6. Click `Add`.

Unity will download the Voxel Editor package from the Git repository and install it automatically.

### Notes

If Unity cannot find Git, the installation may fail with an error such as:

`No 'git' executable was found`

For more information about installing packages from Git URLs, see the Unity documentation:

https://docs.unity3d.com/Manual/upm-ui-giturl.html

---

# Usage

## 1. Create a VoxelWorld

Create an empty `GameObject` in your scene.

For example:

`GameObject > Create Empty`

Attach VoxelWorld.cs to the GameObject.

The GameObject with VoxelWorld.cs attached will become the parent object for Prefabs placed by the Voxel Editor.

---

## 2. Create a VoxelPrefabDatabase

Create a VoxelPrefabDatabase asset.

In the Project window, select:

`Create > Voxel > VoxelPrefabDatabase`

Register the Prefabs that you want to use in the database.

You can create multiple VoxelPrefabDatabase assets.

For example:

```
- EnvironmentPrefabs
- DecorationPrefabs
- CharacterPrefabs
```

Each database can contain different Prefabs.

---

## 3. Open the Voxel Editor

Select the `Voxel Editor` Tool from the Unity Editor Tool panel.

The Voxel Editor GUI will appear in the Scene View.

The GUI contains functions such as:

```
- Pen
- Eraser
- Grid
- Grid Size
- Rotation
- Prefab selection
- VoxelPrefabDatabase selection
```

---

## 4. Select a VoxelPrefabDatabase

If multiple `VoxelPrefabDatabase` assets exist, select the database that you want to use from the database selection area in the Voxel Editor GUI.

The Prefabs registered in the selected database will be displayed in the Prefab selection area.

---

## 5. Select a Prefab

Select a Prefab from the Prefab thumbnail list.

The selected Prefab will be highlighted with a yellow border.

The selected Prefab will be used when placing blocks.

---

## 6. Place a Prefab

Select `Pen` mode.

Move the mouse over the Scene View.

The placement position will be displayed as a preview.

Click the desired position to place the selected Prefab.

The Prefab will automatically be aligned to the voxel grid.

Placed Prefabs will be generated as child objects of the GameObject with `VoxelWorld.cs` attached.

---

## 7. Drag Placement

Enable the `Pen Drag` option.

Click and drag in the Scene View.

Prefabs will be placed continuously while dragging.

The editor determines the next placement position based on the cursor movement and the surrounding voxel positions.

---

## 8. Erase Prefabs

Select `Eraser` mode.

Move the mouse over a placed Prefab.

The target Prefab will be highlighted.

Click the Prefab to remove it.

---

## 9. Drag Erasing

Enable the `Eraser Drag` option.

Click and drag over placed Prefabs.

Prefabs will be removed continuously while dragging.

---

# Grid

## Grid ON/OFF

Use the `Grid button` to switch the grid display between `ON` and `OFF`.

When Grid is `OFF`, the Grid Size controls will be hidden.

When Grid is `ON`, the Grid Size controls will be displayed.

---

## Grid Size

The `Grid Size` can be changed using the `-` and `+` buttons.

The `Grid Size` changes dynamically in the Scene View.

A larger value creates a larger grid.

---

# Prefab Rotation

The Rotation section can be expanded or collapsed using the Rotation button.

The selected Prefab can be rotated by `90 degrees` around the following axes:

```
- X axis
- Y axis
- Z axis
```

The current rotation is displayed in the `Rotation` section.

The rotation is applied when placing the Prefab.

---

# Multiple VoxelPrefabDatabase Support

Multiple `VoxelPrefabDatabase` assets can be created and used in the same project.

Each database can contain a different collection of Prefabs.

For example:

Environment Database

```
- Grass
- Dirt
- Stone
- Sand
```

Building Database

```
- Wall
- Floor
- Roof
- Door
```

Decoration Database

```
- Tree
- Rock
- Flower
- Lamp
```

You can switch between these databases from the Voxel Editor GUI.

This allows you to organize large numbers of Prefabs into separate categories.

---

# Hierarchy Structure

Placed Prefabs are automatically generated under the GameObject that has `VoxelWorld.cs` attached.

For example:

```
Scene 
    ∟ VoxelWorld
            ∟ Grass
            ∟ Stone
            ∟ Dirt
            ∟ Tree
            ∟ Rock
```

This keeps the Hierarchy organized and makes it easier to manage voxel objects.

---

# Undo and Redo

The Voxel Editor supports Unity Editor `Undo` and `Redo`.

You can undo placement and deletion operations using the standard Unity commands.

For example:

`Ctrl + Z`

or:

`Edit > Undo`

Redo can also be performed using the standard Unity Editor commands.

---

# Recommended Project Structure

A recommended project structure is:

```
Assets
    ∟ VoxelEditor
            ∟ Editor
            ∟ Runtime
            ∟ Documentation
            ∟ Samples
```
Your project's own `Prefabs` and `VoxelPrefabDatabase` assets can be stored separately.

For example:

```
Assets
    ∟ VoxelEditor
    ∟ Game
        ∟ Prefabs
        ∟ VoxelDatabases
        ∟ Scenes
```

This makes it easier to distinguish the Voxel Editor package from your game's assets.

---

# Notes

The Voxel Editor is an Editor Tool.

The editor functionality is executed inside the Unity Editor and is not required during gameplay.

VoxelWorld.cs and other Runtime scripts are used by the generated voxel data and scene objects.

Editor-only scripts should remain inside an Editor folder so that they are not included in the final build.

author：Momiji

---

# 日本語

# Voxel Editor

3Dグリッド上にVoxel形式のPrefabを簡単に配置するためのUnity Editor Toolです。

## 機能

- 3Dグリッド上へのVoxel形式Prefabの配置
- 配置したPrefabの削除
- ペンモードと消しゴムモード
- ドラッグによる連続配置
- ドラッグによる連続削除
- X、Y、Z軸方向へのPrefab回転
- Gridの表示・非表示
- Grid Sizeの調整
- Prefabサムネイルによる選択
- 複数のVoxelPrefabDatabaseへの対応
- Editor GUI上でのPrefabデータベース切り替え
- 配置したPrefabをVoxelWorld GameObjectの子オブジェクトとして自動整理
- Unity EditorのUndo/Redoへの対応
- ScriptableObjectを使用したPrefabデータベースへのPrefab登録

---

# インストール方法

## 方法1: Unity Packageとしてインストールする

1. Voxel Editorのパッケージをダウンロードします。

2. Voxel EditorをインストールしたいUnityプロジェクトを開きます。

3. 以下を選択します。

   `Window > Package Manager`

4. Package Managerウィンドウ左上の`+`ボタンをクリックします。

5. 以下を選択します。

   `Install package from disk...`

6. Voxel Editorのパッケージに含まれている`package.json`を選択します。

7. Unityが自動的にパッケージをインポートします。

インストールが完了すると、Unity EditorでVoxel Editor Toolを使用できるようになります。

---

## 方法2: Unity Packageをインポートする

`.unitypackage`形式でパッケージが提供されている場合は、以下の手順でインポートできます。

1. Unityプロジェクトを開きます。

2. 以下を選択します。

   `Assets > Import Package > Custom Package...`

3. Voxel Editorの`.unitypackage`ファイルを選択します。

4. インポートするファイルを確認します。

5. Importをクリックします。

Voxel Editorがプロジェクトにインポートされます。

---

## 方法3: Git URLからインストールする

`Unity Package Manager`を使用すると、GitリポジトリからVoxel Editorを直接インストールできます。

### 必要条件

コンピューターにGitがインストールされている必要があります。

リポジトリでGit LFSを使用している場合は、Git LFSもインストールする必要があります。

### インストール手順

1. Voxel EditorをインストールしたいUnityプロジェクトを開きます。

2. 以下を開きます。

   `Window > Package Manager`

3. Package Managerウィンドウ左上の`+`ボタンをクリックします。

4. 以下を選択します。

   `Add package from git URL`

5. 以下のGit URLを入力します。

   `https://github.com/momiji1107/VoxelEditor.git?path=/Assets/VoxelEditor`

6. `Add`をクリックします。

UnityがGitリポジトリからVoxel Editorパッケージをダウンロードし、自動的にインストールします。

### 注意事項

UnityがGitを認識できない場合、以下のようなエラーが表示されてインストールに失敗することがあります。

`No 'git' executable was found`

Git URLからのパッケージインストールについて詳しくは、Unity公式ドキュメントを参照してください。

https://docs.unity3d.com/Manual/upm-ui-giturl.html

---

# 使用方法

## 1. VoxelWorldを作成する

シーン内に空のGameObjectを作成します。

例えば、以下を選択します。

`GameObject > Create Empty`

作成したGameObjectに`VoxelWorld.cs`をアタッチします。

`VoxelWorld.cs`がアタッチされたGameObjectは、Voxel Editorによって配置されるPrefabの親オブジェクトになります。

---

## 2. VoxelPrefabDatabaseを作成する

`VoxelPrefabDatabase`アセットを作成します。

Projectウィンドウで以下を選択します。

`Create > Voxel > VoxelPrefabDatabase`

使用したいPrefabをデータベースに登録します。

`VoxelPrefabDatabase`は複数作成することができます。

例えば、以下のように分けることができます。

```
- EnvironmentPrefabs
- DecorationPrefabs
- CharacterPrefabs
```

それぞれのデータベースに異なるPrefabを登録できます。

---

## 3. Voxel Editorを開く

Unity EditorのToolパネルから`Voxel Editor` Toolを選択します。

Scene ViewにVoxel EditorのGUIが表示されます。

GUIには以下のような機能があります。

```
- Pen
- Eraser
- Grid
- Grid Size
- Rotation
- Prefab選択
- VoxelPrefabDatabase選択
```

---

## 4. VoxelPrefabDatabaseを選択する

複数の`VoxelPrefabDatabase`が存在する場合、Voxel Editor GUIのデータベース選択欄から使用したいデータベースを選択します。

選択したデータベースに登録されているPrefabがPrefab選択欄に表示されます。

---

## 5. Prefabを選択する

Prefabのサムネイル一覧から使用したいPrefabを選択します。

選択中のPrefabには黄色い枠線が表示されます。

選択したPrefabがブロック配置時に使用されます。

---

## 6. Prefabを配置する

`Pen`モードを選択します。

Scene View上でマウスを動かします。

Prefabの配置位置がプレビュー表示されます。

配置したい場所をクリックすると、選択中のPrefabが配置されます。

Prefabは自動的にVoxel Gridに合わせて配置されます。

配置されたPrefabは、`VoxelWorld.cs`がアタッチされたGameObjectの子オブジェクトとして生成されます。

---

## 7. ドラッグ配置

`ペンドラッグ`機能をONにします。

Scene View上でクリックしたままドラッグします。

ドラッグ中、Prefabが連続して配置されます。

エディタはカーソルの移動方向と周囲のVoxelの位置を使用して、次に配置する位置を決定します。

---

## 8. Prefabを削除する

`Eraser`モードを選択します。

Scene View上で配置済みのPrefabにマウスを移動します。

削除対象のPrefabがハイライト表示されます。

Prefabをクリックすると削除されます。

---

## 9. ドラッグ削除

`消しゴムドラッグ`機能をONにします。

配置済みのPrefab上をクリックしたままドラッグします。

ドラッグしたPrefabが連続して削除されます。

---

# Grid

## Gridの表示・非表示

`Grid`ボタンを使用してGridの表示・非表示を切り替えられます。

Gridが`OFF`の場合、Grid Sizeの設定欄は`非表示`になります。

Gridが`ON`の場合、Grid Sizeの設定欄が`表示`されます。

---

## Grid Size

`Grid Size`は`-`ボタンと`+`ボタンを使用して変更できます。

`Grid Size`を変更すると、Scene View上のGridサイズが動的に変化します。

値を大きくすると、より広い範囲のGridが表示されます。

---

# Prefabの回転

Rotationボタンを使用してRotation設定を展開・折りたたみできます。

選択中のPrefabを以下の軸方向に90度ずつ回転できます。

```
- X軸
- Y軸
- Z軸
```

現在の回転角度は`Rotation`欄に表示されます。

設定した回転はPrefabを配置するときに適用されます。

---

# 複数のVoxelPrefabDatabaseへの対応

同じプロジェクト内に複数の`VoxelPrefabDatabase`を作成して使用できます。

それぞれのデータベースに異なるPrefabを登録できます。

例えば、以下のように分類できます。

Environment Database

```
- Grass
- Dirt
- Stone
- Sand
```

Building Database

```
- Wall
- Floor
- Roof
- Door
```

Decoration Database

```
- Tree
- Rock
- Flower
- Lamp
```

これらのデータベースはVoxel Editor GUIから切り替えることができます。

大量のPrefabをカテゴリーごとに整理して管理することができます。

---

# Hierarchyの構造

配置したPrefabは、`VoxelWorld.cs`がアタッチされたGameObjectの子オブジェクトとして自動的に生成されます。

例えば、以下のようなHierarchyになります。

```
Scene
    ∟ VoxelWorld
            ∟ Grass
            ∟ Stone
            ∟ Dirt
            ∟ Tree
            ∟ Rock
```

これによりHierarchyを整理しやすくなり、Voxelオブジェクトを管理しやすくなります。

---

# UndoとRedo

Voxel EditorはUnity Editorの`Undo`と`Redo`に対応しています。

Prefabの配置や削除などの操作をUnity標準の操作で取り消すことができます。

例えば:

`Ctrl + Z`

または:

`Edit > Undo`

RedoもUnity Editor標準の操作で実行できます。

---

# 推奨プロジェクト構成

以下のようなプロジェクト構成を推奨します。

```
Assets
    ∟ VoxelEditor
            ∟ Editor
            ∟ Runtime
            ∟ Documentation
            ∟ Samples
```

ゲーム本体で使用する`Prefab`や`VoxelPrefabDatabase`は別の場所に保存できます。

例えば:

```
Assets
    ∟ VoxelEditor
    ∟ Game
        ∟ Prefabs
        ∟ VoxelDatabases
        ∟ Scenes
```

このように分けることで、Voxel Editorのパッケージとゲーム本体のアセットを区別しやすくなります。

---

# 注意事項

Voxel EditorはEditor Toolです。

エディタ機能はUnity Editor内で実行されるため、ゲームプレイ中にEditor機能を使用する必要はありません。

`VoxelWorld.cs`などのRuntimeスクリプトは、生成されたVoxelデータやシーン上のオブジェクトで使用されます。

Editor専用のスクリプトはEditorフォルダ内に配置し、最終的なゲームビルドに含まれないようにしてください。

作者：もみじ
