# Voxel Editor

A Unity Editor Tool for easily placing voxel-style Prefabs on a 3D grid.

Voxel Editor allows you to place, rotate, and erase Prefabs directly in the Unity Scene View, similar to a 3D Tilemap.

The editor supports not only cube-shaped Prefabs, but also rectangular and multi-cell Prefabs with different dimensions.

---

# Features

- Place Prefabs on a 3D grid.
- Support for cube-shaped and non-cubic Prefabs.
- Support for Prefabs occupying multiple grid cells.
- Preserve the original Prefab scale.
- Automatically determine the grid cells occupied by a Prefab.
- Correctly handle Prefab rotation when determining occupied grid cells.
- Prefab rotation on the X, Y, and Z axes.
- Pen mode and Eraser mode.
- Drag placement and drag erasing.
- Placement preview in the Scene View.
- Grid display ON/OFF.
- Adjustable Cell Size.
- Grid cell size automatically follows the Cell Size.
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

5. Click `Import`.

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

5. Enter the Voxel Editor Git repository URL.

6. Click `Add`.

Unity will download the Voxel Editor package from the Git repository and install it automatically.

### Notes

If Unity cannot find Git, the installation may fail with an error such as:

`No 'git' executable was found`

For more information about installing packages from Git URLs, see the Unity documentation.

---

# Usage

## 1. Create a VoxelWorld

Create an empty `GameObject` in your scene.

For example:

`GameObject > Create Empty`

Attach `VoxelWorld.cs` to the GameObject.

The GameObject with `VoxelWorld.cs` attached becomes the parent object for Prefabs placed by the Voxel Editor.

---

## 2. Set the Cell Size

Select the GameObject with `VoxelWorld.cs` attached.

The `Cell Size` can be configured from the `VoxelWorld` Inspector.

The Cell Size determines the size of one grid cell in the Scene View.

For example:

```
Cell Size = 1

One grid cell:
1 × 1 × 1 world units
```

If the Cell Size is changed:

```
Cell Size = 2

One grid cell:
2 × 2 × 2 world units
```

The Scene View grid and Prefab placement use the configured Cell Size.

### Important

The Cell Size controls the size of the voxel grid.

It does not change the original Scale of the Prefab.

This allows Prefabs with different dimensions to be used in the same VoxelWorld.

---

## 3. Create a VoxelPrefabDatabase

Create a `VoxelPrefabDatabase` asset.

In the Project window, select:

`Create > Voxel > VoxelPrefabDatabase`

Register the Prefabs that you want to use in the database.

You can create multiple VoxelPrefabDatabase assets.

For example:

```
- EnvironmentPrefabs
- BuildingPrefabs
- DecorationPrefabs
```

Each database can contain different Prefabs.

---

## 4. Open the Voxel Editor

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

## 5. Select a VoxelPrefabDatabase

If multiple `VoxelPrefabDatabase` assets exist, select the database that you want to use from the database selection area in the Voxel Editor GUI.

The Prefabs registered in the selected database will be displayed in the Prefab selection area.

---

## 6. Select a Prefab

Select a Prefab from the Prefab thumbnail list.

The selected Prefab will be highlighted with a yellow border.

The selected Prefab will be used when placing objects.

Voxel Editor supports Prefabs with different dimensions.

For example:

```
1 × 1 × 1
2 × 1 × 1
1 × 2 × 1
2 × 2 × 1
2 × 2 × 2
1 × 2 × 3
```

A Prefab does not have to be a cube.

---

## 7. Place a Prefab

Select `Pen` mode.

Move the mouse over the Scene View.

A placement preview will be displayed.

The preview shows where the selected Prefab will be placed according to the current grid and rotation settings.

Click the desired position to place the Prefab.

The Prefab will be aligned to the VoxelWorld grid.

Placed Prefabs will be generated as child objects of the GameObject with `VoxelWorld.cs` attached.

---

## 8. Prefab Size and Grid Occupancy

Voxel Editor determines how many grid cells a Prefab occupies based on its dimensions.

For example, if the Cell Size is `1`:

```
2 × 2 × 2 Prefab

┌───┬───┐
│   │   │
├───┼───┤
│   │   │
└───┴───┘

The Prefab occupies:

2 × 2 × 2 grid cells
```

A rectangular Prefab can therefore occupy multiple cells along different axes.

The Prefab's original Scale is preserved.

The grid is used to determine placement and occupied positions rather than forcing every Prefab to be a 1 × 1 × 1 cube.

---

## 9. Prefab Rotation

The Rotation section can be expanded or collapsed using the Rotation button.

The selected Prefab can be rotated by `90 degrees` around the following axes:

```
- X axis
- Y axis
- Z axis
```

The current rotation is displayed in the `Rotation` section.

Rotation affects the Prefab's orientation and its occupied grid cells.

For example, a rectangular Prefab:

```
Before rotation:

2 × 1 × 1
```

may occupy:

```
1 × 2 × 1
```

after a 90-degree rotation.

Voxel Editor takes the rotated dimensions into account when checking whether the Prefab can be placed.

The placement preview and actual placement use the same rotation settings.

---

## 10. Drag Placement

Enable the `Pen Drag` option.

Click and drag in the Scene View.

Prefabs will be placed continuously while dragging.

The editor determines the next placement position based on cursor movement and the surrounding grid positions.

Drag placement can be used in different directions, including vertical movement.

---

## 11. Erase Prefabs

Select `Eraser` mode.

Move the mouse over a placed Prefab.

The target Prefab will be highlighted.

Click the Prefab to remove it.

The entire placed Prefab is removed as one object.

---

## 12. Drag Erasing

Enable the `Eraser Drag` option.

Click and drag over placed Prefabs.

Prefabs will be removed continuously while dragging.

---

# Grid

## Grid ON/OFF

Use the `Grid` button to switch the grid display between `ON` and `OFF`.

When Grid is `OFF`, the Grid Size controls will be hidden.

When Grid is `ON`, the Grid Size controls will be displayed.

The grid is a visual guide for editing the VoxelWorld.

---

## Grid Size

The `Grid Size` controls the visible range of the grid in the Scene View.

The `Grid Size` can be changed using the `-` and `+` buttons.

A larger value displays a larger grid area.

### Cell Size vs Grid Size

These two settings have different purposes.

| Setting | Purpose |
|---|---|
| Cell Size | Determines the world-space size of one grid cell |
| Grid Size | Determines how large an area of the grid is displayed |

For example:

```
Cell Size = 1
Grid Size = 40
```

means that each grid cell is 1 world unit in size, while the editor displays a grid covering the configured grid range.

Changing `Grid Size` does not change the size of Prefabs or grid cells.

---

# Multiple VoxelPrefabDatabase Support

Multiple `VoxelPrefabDatabase` assets can be created and used in the same project.

Each database can contain a different collection of Prefabs.

For example:

### Environment Database

```
- Grass
- Dirt
- Stone
- Sand
```

### Building Database

```
- Wall
- Floor
- Roof
- Door
```

### Decoration Database

```
- Tree
- Rock
- Flower
- Lamp
```

You can switch between these databases from the Voxel Editor GUI.

This allows you to organize large numbers of Prefabs into separate categories.

---

# Prefab Rotation and Occupied Grid

Voxel Editor considers the Prefab's rotated dimensions when determining occupied grid positions.

For example, consider a Prefab with dimensions:

```
2 × 1 × 3
```

Depending on its rotation, the dimensions along the grid axes can change.

Voxel Editor uses the rotated dimensions when checking the occupied positions.

This prevents the editor from treating a rotated rectangular Prefab as though it were still using its original orientation.

The same occupied-grid calculation is used for placement validation and the placement preview.

---

# Placement Preview

When using Pen mode, Voxel Editor displays a preview of the selected Prefab before placing it.

The preview takes the following settings into account:

```
- VoxelWorld Cell Size
- Prefab dimensions
- Prefab rotation
- Grid position
- Existing occupied grid positions
```

This allows you to check the placement position before clicking.

The preview is also useful when working with large or non-cubic Prefabs.

---

# Hierarchy Structure

Placed Prefabs are automatically generated under the GameObject that has `VoxelWorld.cs` attached.

For example:

```
Scene
    ∟ VoxelWorld
            ∟ Grass
            ∟ Stone
            ∟ Wall
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

On macOS, use the standard Unity Editor shortcut for Undo.

Redo can also be performed using the standard Unity Editor commands.

---

# Recommended Prefab Setup

Voxel Editor can work with both cube-shaped and non-cubic Prefabs.

For the most predictable results, make sure the Prefab's model and transform are set up correctly before registering it in a `VoxelPrefabDatabase`.

For example:

```
Assets
    ∟ Game
        ∟ Prefabs
            ∟ Grass.prefab
            ∟ Wall.prefab
            ∟ Tree.prefab
            ∟ Building.prefab
```

A Prefab can have dimensions such as:

```
1 × 1 × 1
2 × 1 × 1
2 × 2 × 1
2 × 2 × 2
1 × 2 × 3
```

The Prefab does not need to be a perfect cube.

---

# Recommended Project Structure

A recommended project structure is:

```
Assets
    ∟ VoxelEditor
            ∟ Editor
            ∟ Runtime
            ∟ Samples
```

Your project's own Prefabs and `VoxelPrefabDatabase` assets can be stored separately.

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

Voxel Editor is an Editor Tool.

The editor functionality is executed inside the Unity Editor and is not required during gameplay.

`VoxelWorld.cs` and other Runtime scripts are used by the generated voxel data and scene objects.

Editor-only scripts should remain inside an `Editor` folder so that they are not included in the final build.

When using non-cubic Prefabs, make sure the Prefab's dimensions and Transform are configured as intended before registering the Prefab in a `VoxelPrefabDatabase`.

Changing the `Cell Size` changes the size of the VoxelWorld grid cells. It does not modify the original Scale of registered Prefabs.

---

# Author

Momiji

---

---

# 日本語

# Voxel Editor

3Dグリッド上にPrefabを簡単に配置するためのUnity Editor Toolです。

Unityの3D Tilemapのように、Scene View上でPrefabの配置・回転・削除を行うことができます。

立方体のPrefabだけでなく、長方形・直方体などの**非立方体Prefab**にも対応しており、Prefabの大きさに応じて複数のGridセルを占有する配置にも対応しています。

---

# 機能

- 3Dグリッド上へのPrefabの配置
- 立方体以外のPrefabへの対応
- 複数のGridセルを占有するPrefabへの対応
- Prefab本来のScaleを維持した配置
- Prefabの大きさに応じた占有Gridセルの自動判定
- 回転後のPrefabの占有Gridセルを考慮した配置
- X、Y、Z軸方向へのPrefab回転
- Penモード
- Eraserモード
- ドラッグによる連続配置
- ドラッグによる連続削除
- Scene View上での配置プレビュー
- Gridの表示・非表示
- Cell Sizeの変更
- Cell Sizeに応じたGridセルサイズの自動変更
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

5. `Import`をクリックします。

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

5. Voxel EditorのGitリポジトリURLを入力します。

6. `Add`をクリックします。

UnityがGitリポジトリからVoxel Editorパッケージをダウンロードし、自動的にインストールします。

### 注意事項

UnityがGitを認識できない場合、以下のようなエラーが表示されてインストールに失敗することがあります。

`No 'git' executable was found`

Git URLからのパッケージインストールについて詳しくは、Unity公式ドキュメントを参照してください。

---

# 使用方法

## 1. VoxelWorldを作成する

シーン内に空のGameObjectを作成します。

例えば、以下を選択します。

`GameObject > Create Empty`

作成したGameObjectに`VoxelWorld.cs`をアタッチします。

`VoxelWorld.cs`がアタッチされたGameObjectは、Voxel Editorによって配置されるPrefabの親オブジェクトになります。

---

## 2. Cell Sizeを設定する

`VoxelWorld.cs`をアタッチしたGameObjectを選択します。

`VoxelWorld`のInspectorから`Cell Size`を設定できます。

`Cell Size`は、1つのGridセルがワールド上でどれだけの大きさになるかを決定します。

例えば、

```
Cell Size = 1

1 Gridセル
= 1 × 1 × 1 ワールド単位
```

の場合、

```
Cell Size = 2

1 Gridセル
= 2 × 2 × 2 ワールド単位
```

となります。

Cell Sizeを変更すると、Scene ViewのGridおよびPrefabの配置基準も変更されます。

### 重要

`Cell Size`はVoxel Gridのセルサイズを変更するための設定です。

Prefab本来のScaleを変更するものではありません。

そのため、同じVoxelWorld内で異なる大きさのPrefabを使用することができます。

---

## 3. VoxelPrefabDatabaseを作成する

`VoxelPrefabDatabase`アセットを作成します。

Projectウィンドウで以下を選択します。

`Create > Voxel > VoxelPrefabDatabase`

使用したいPrefabをデータベースに登録します。

`VoxelPrefabDatabase`は複数作成することができます。

例えば、以下のように分類できます。

```
- EnvironmentPrefabs
- BuildingPrefabs
- DecorationPrefabs
```

それぞれのデータベースに異なるPrefabを登録できます。

---

## 4. Voxel Editorを開く

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

## 5. VoxelPrefabDatabaseを選択する

複数の`VoxelPrefabDatabase`が存在する場合、Voxel Editor GUIのデータベース選択欄から使用したいデータベースを選択します。

選択したデータベースに登録されているPrefabがPrefab選択欄に表示されます。

---

## 6. Prefabを選択する

Prefabのサムネイル一覧から使用したいPrefabを選択します。

選択中のPrefabには黄色い枠線が表示されます。

選択したPrefabが配置時に使用されます。

Voxel Editorでは、立方体以外のPrefabも使用できます。

例えば、

```
1 × 1 × 1
2 × 1 × 1
1 × 2 × 1
2 × 2 × 1
2 × 2 × 2
1 × 2 × 3
```

などの大きさのPrefabを配置できます。

---

## 7. Prefabを配置する

`Pen`モードを選択します。

Scene View上でマウスを動かします。

選択中のPrefabの配置位置がプレビュー表示されます。

配置したい場所をクリックするとPrefabが配置されます。

PrefabはVoxelWorldのGridに合わせて配置されます。

配置されたPrefabは、`VoxelWorld.cs`がアタッチされたGameObjectの子オブジェクトとして生成されます。

---

## 8. Prefabの大きさと占有Grid

Voxel EditorはPrefabの大きさに応じて、Prefabが占有するGridセルを判定します。

例えば、Cell Sizeが`1`の場合、

```
2 × 2 × 2 のPrefab
```

は、

```
2 × 2 × 2 Gridセル
```

を占有します。

つまり、

```
1 × 1 × 1
```

のPrefabだけでなく、

```
2 × 1 × 1
2 × 2 × 1
1 × 2 × 3
```

のようなPrefabも、それぞれの大きさに応じたGridセルを使用して配置されます。

Prefab本来のScaleは変更されません。

---

## 9. Prefabを回転する

`Rotation`ボタンを使用してRotation設定を展開・折りたたみできます。

選択中のPrefabを以下の軸方向に`90度`ずつ回転できます。

```
- X軸
- Y軸
- Z軸
```

現在の回転角度は`Rotation`欄に表示されます。

回転するとPrefabの向きだけでなく、Grid上で占有する範囲も変化します。

例えば、

```
回転前

2 × 1 × 1
```

のPrefabを90度回転させると、

```
1 × 2 × 1
```

としてGrid上の占有範囲が変化する場合があります。

Voxel Editorは回転後のPrefabの大きさを考慮して、配置可能な位置を判定します。

また、配置プレビューと実際の配置で同じ回転設定が使用されます。

---

## 10. ドラッグ配置

`Pen Drag`機能をONにします。

Scene View上でクリックしたままドラッグします。

ドラッグ中、Prefabが連続して配置されます。

エディタはカーソルの移動方向や周囲のGrid位置を使用して、次に配置する位置を決定します。

縦方向を含む複数の方向へのドラッグ配置に対応しています。

---

## 11. Prefabを削除する

`Eraser`モードを選択します。

Scene View上で配置済みのPrefabにマウスを移動します。

削除対象のPrefabがハイライト表示されます。

Prefabをクリックすると削除されます。

複数Gridセルを占有しているPrefabも、1つの配置済みPrefabとして削除されます。

---

## 12. ドラッグ削除

`Eraser Drag`機能をONにします。

配置済みのPrefab上をクリックしたままドラッグします。

ドラッグしたPrefabが連続して削除されます。

---

# Grid

## Gridの表示・非表示

`Grid`ボタンを使用してGridの表示・非表示を切り替えられます。

Gridが`OFF`の場合、Grid Sizeの設定欄は非表示になります。

Gridが`ON`の場合、Grid Sizeの設定欄が表示されます。

GridはScene View上で配置位置を確認するための視覚的なガイドです。

---

## Grid Size

`Grid Size`はScene View上に表示するGridの範囲を設定します。

`Grid Size`は`-`ボタンと`+`ボタンを使用して変更できます。

値を大きくすると、より広い範囲のGridが表示されます。

### Cell SizeとGrid Sizeの違い

`Cell Size`と`Grid Size`は別の設定です。

| 設定 | 役割 |
|---|---|
| Cell Size | 1つのGridセルのワールド上の大きさ |
| Grid Size | Scene Viewに表示するGridの範囲 |

例えば、

```
Cell Size = 1
Grid Size = 40
```

の場合、

`1 Gridセル = 1ワールド単位`

となり、そのうえで設定された範囲のGridがScene Viewに表示されます。

`Grid Size`を変更しても、Gridセルそのものの大きさやPrefabのScaleは変更されません。

---

# 複数のVoxelPrefabDatabaseへの対応

同じプロジェクト内に複数の`VoxelPrefabDatabase`を作成して使用できます。

それぞれのデータベースに異なるPrefabを登録できます。

例えば、以下のように分類できます。

### Environment Database

```
- Grass
- Dirt
- Stone
- Sand
```

### Building Database

```
- Wall
- Floor
- Roof
- Door
```

### Decoration Database

```
- Tree
- Rock
- Flower
- Lamp
```

これらのデータベースはVoxel Editor GUIから切り替えることができます。

大量のPrefabをカテゴリーごとに整理して管理することができます。

---

# 回転と占有Grid

Voxel Editorは、Prefabを回転させた後の大きさを考慮して、Grid上での占有位置を判定します。

例えば、

```
2 × 1 × 3
```

のPrefabは、回転によってGridの各軸方向に対する大きさが変化します。

Voxel Editorは回転後の大きさを使用して、Prefabが占有するGrid位置を判定します。

これにより、回転前のPrefabサイズだけを使用して配置可能かどうかを判定することを防ぎます。

配置プレビューと実際の配置でも、同じ占有Gridの判定が使用されます。

---

# 配置プレビュー

Penモードでは、Prefabを配置する前にScene View上で配置位置をプレビューできます。

プレビューでは、以下の要素が考慮されます。

```
- VoxelWorldのCell Size
- Prefabの大きさ
- Prefabの回転
- Grid上の配置位置
- 既に使用されているGrid位置
```

そのため、クリックして配置する前にPrefabの配置位置を確認できます。

特に大きなPrefabや非立方体Prefabを配置するときに便利です。

---

# Hierarchyの構造

配置したPrefabは、`VoxelWorld.cs`がアタッチされたGameObjectの子オブジェクトとして自動的に生成されます。

例えば、以下のようなHierarchyになります。

```
Scene
    ∟ VoxelWorld
            ∟ Grass
            ∟ Stone
            ∟ Wall
            ∟ Tree
            ∟ Rock
```

これによりHierarchyを整理しやすくなり、配置したVoxelオブジェクトを管理しやすくなります。

---

# UndoとRedo

Voxel EditorはUnity Editorの`Undo`と`Redo`に対応しています。

Prefabの配置や削除などの操作をUnity標準の操作で取り消すことができます。

例えば:

`Ctrl + Z`

または:

`Edit > Undo`

macOSではUnity Editor標準のUndoショートカットを使用してください。

RedoもUnity Editor標準の操作で実行できます。

---

# Prefab作成時の推奨事項

Voxel Editorでは立方体Prefabだけでなく、非立方体Prefabも使用できます。

より意図した結果で使用するため、Prefabを`VoxelPrefabDatabase`に登録する前に、モデルやTransformを適切に設定してください。

例えば:

```
Assets
    ∟ Game
        ∟ Prefabs
            ∟ Grass.prefab
            ∟ Wall.prefab
            ∟ Tree.prefab
            ∟ Building.prefab
```

以下のようなPrefabも使用できます。

```
1 × 1 × 1
2 × 1 × 1
2 × 2 × 1
2 × 2 × 2
1 × 2 × 3
```

Prefabは必ずしも完全な立方体である必要はありません。

---

# 推奨プロジェクト構成

以下のようなプロジェクト構成を推奨します。

```
Assets
    ∟ VoxelEditor
            ∟ Editor
            ∟ Runtime
            ∟ Samples
```

ゲーム本体で使用するPrefabや`VoxelPrefabDatabase`は別の場所に保存できます。

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

エディタ機能はUnity Editor内で実行されるため、ゲームプレイ中にVoxel EditorのEditor機能を使用する必要はありません。

`VoxelWorld.cs`などのRuntimeスクリプトは、生成されたVoxelデータやシーン上のオブジェクトで使用されます。

Editor専用のスクリプトは`Editor`フォルダ内に配置し、最終的なゲームビルドに含まれないようにしてください。

非立方体Prefabを使用する場合は、`VoxelPrefabDatabase`に登録する前にPrefabの大きさやTransformが意図した状態になっていることを確認してください。

`Cell Size`を変更するとVoxelWorldのGridセルの大きさが変わりますが、登録済みPrefabの元のScale自体は変更されません。

---

# Author

Momiji