﻿// ModelMod: 3d data snapshotting & substitution program.
// Copyright(C) 2015,2016 John Quigley

// This program is free software : you can redistribute it and / or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 2.1 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with this program.If not, see <http://www.gnu.org/licenses/>.

namespace ModelMod

open SharpDX.Direct3D9 

// Shorthand type defs

type SDXVertexElement = SharpDX.Direct3D9.VertexElement
type SDXVertexDeclUsage = SharpDX.Direct3D9.DeclarationUsage
type SDXVertexDeclType = SharpDX.Direct3D9.DeclarationType

/// Contains the name of all available input profiles.  An input profile is just a set of keybindings for 
/// controlling ModelMod in games.  Different games and systems require different input layouts, so that 
/// ModelMod doesn't interfere too much with the game.  Some games make heavy use of the F keys, for instance,
/// so the punctuation layout is a better choice.  Its expected that some games won't work well with either of 
/// these, and some new layouts will need to be defined.
/// The managed code generally doesn't care about the precise definition of each layout, with the exception of 
/// The launcher app, which describes the layout in the UI.  Native code (specificaly RenderState.cpp) is 
/// responsible for actually setting up the bindings.
module InputProfiles = 
    let PunctRock = "PunctuationKeys"
    let FItUp = "FKeys"
    
    let ValidProfiles = [ PunctRock; FItUp ]

    let DefaultProfile = FItUp

    let isValid (profile:string) =
        ValidProfiles |> List.exists (fun p -> p.ToLowerInvariant() = profile.ToLowerInvariant())

module CoreTypes =
    // ------------------------------------------------------------------------

    /// Shorthand for Microsoft.Xna.Framework.Vector2
    type Vec2F = Microsoft.Xna.Framework.Vector2

    /// Shorthand for Microsoft.Xna.Framework.Vector3
    type Vec3F = Microsoft.Xna.Framework.Vector3

    /// Shorthand for Microsoft.Xna.Framework.Vector4
    type Vec4F = Microsoft.Xna.Framework.Vector4

    /// A 4-element vector of whatever.  Handy for keeping around data baggage, but doesn't define any
    /// normal vector ops (addition, dot product, etc).
    type Vec4X(x,y,z,w) =
        member v.X = x
        member v.Y = y
        member v.Z = z
        member v.W = w

    // ------------------------------------------------------------------------
    // Configuration types

    /// Contains settings specific to a particular game.  Usually these settings relate to how a game lays out geometry data
    /// in D3D memory.
    type GameProfile = {
        /// Controls the order in which normal vector components are written to D3D buffers.
        /// False: XYZW; True: ZYXW
        ReverseNormals: bool
    }

    let DefaultGameProfile = {
        ReverseNormals = false
    }

    /// A run config for modelmod.  These are stored in the registry.
    type RunConfig = {
        /// Reg key that this profile is stored under, e.g "Profile0000"
        ProfileKeyName: string 
        /// Friendly name for profile (if missing, defaults to exe base name)
        ProfileName: string 
        /// Path to exe
        ExePath: string 
        /// If true, mods will be load and displayed on startup; otherwise they must be loaded
        /// and displayed manually with keyboard commands
        LoadModsOnStart: bool
        /// Whether the current/next run is in full (snapshot) mode or playback only.  This is really for 
        /// future use, since right now we don't have a separate "playback" mode (the idea is that we could be 
        /// slightly more efficient by disabling certain things, like shader constant tracking,
        /// when we know we are only playing back mods).
        RunModeFull: bool 
        /// Input profile (i.e.: input key layout)
        InputProfile: string 
        /// Snapshot profile (i.e.: model transforms for snapshot) 
        SnapshotProfile: string 
        /// Game profile 
        GameProfile: GameProfile
        /// Doc root for this profile.  Currently ignored.
        DocRoot: string 
        /// Period of time that the Loader will wait for the game to start before exiting.
        LaunchWindow: int
    } 

    /// When no run configuration is available in the registry, this is what is used.  The Input and Snapshot 
    /// modules define their own defaults.  The default DocRoot is <MyDocuments>\ModelMod.
    let DefaultRunConfig = {
        ProfileKeyName = ""
        ProfileName = ""
        ExePath = ""
        RunConfig.RunModeFull = true
        LoadModsOnStart = true
        InputProfile = ""
        SnapshotProfile = ""
        GameProfile = DefaultGameProfile
        DocRoot = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),"ModelMod")
        LaunchWindow = 15
    }

    // ------------------------------------------------------------------------
    // Mod and ref data

    /// List of available mod types.  Really this is "file type", since a reference isn't really a mod (though you can
    /// define things like vertex group exclusions in a reference).
    /// "Replacement" mods replace the original game data; "Addition" mods draw the mod on top of the original game data.
    type ModType = 
        /// Animated on the GPU; mod is expected to contain at least blend index data and usually blend weights as well.
        /// Mesh data is also usually not scaled or translated in world space in any way.
        GPUReplacement 
        /// Animated on the CPU.  A snapshot of this kind of data usually results in a fully world-transformed and 
        /// animated mesh - pretty useless for modding.  ModelMod doesn't not currently support this type of mod,
        /// even though it is technically possible.
        | CPUReplacement 
        /// Removal mod.  These don't define meshes, but instead just list a primitive and vertex count.  Whenever 
        /// _anything_ is drawn with that exact primitive and vert count, it is not displayed.  This can lead to some
        /// artifacts (especially with things like particle emitters which have very regular and common low-numbered 
        /// vert/primitive counts), so should be used with care.
        | Deletion 
        /// Reference.  Generally you don't want to touch the reference file.  However, it is possible to change it 
        /// so that you can do vertex inclusion/exclusion groups.
        | Reference 

    /// These types control how weighting is done in MeshRelation, in particular where the 
    /// blend indices and blend weights are expected to be found.
    type WeightMode = 
        /// Get blend data from mod mesh.  Mod author must ensure that all verts are propertly 
        /// weighted in the 3d tool.  This can be tedious, especially with symmetric parts, so this 
        /// mode is primarily here for advanced users and control freaks.
        Mod 

        /// Get blend data from the ref.  This is the default and easiest mode to use.
        | Ref 
        /// Get blend data from the binary ref data.  This is mostly a developer debug mode - it doesn't 
        /// support vertex annotation group filtering, amongst other limitations.
        | BinaryRef

    /// Record which contains indices for position, texture coordinate and normal.  Useful for obj-style meshes.
    type PTNIndex = { Pos: int; Tex: int; Nrm: int }

    /// A triangle composed of PTNIndex recrods.
    type IndexedTri = {
        /// Always 3 elements long
        Verts: PTNIndex[] 
    }

    /// The vertex declaration specifies the D3D vertex layout; it is usually captured at snapshot
    /// time, and then used at playback time to create the required format.  Generally you shouldn't modify it
    /// Both the raw bytes and the "unpacked" list are included in this type.
    type VertexDeclarationData = byte[] * SDXVertexElement list

    /// This allows raw vertex data from a snapshot to be reloaded and displayed.  Debug feature.
    type BinaryVertexData = {
        NumVerts: uint32
        Stride: uint32
        Data: byte[]
    }

    /// Various options for reading meshes.
    type MeshReadFlags = {
        /// Whether to read any associated .mtl files.  If true and an .mtl file is available, Tex0Path
        /// will be set to whatever texture is defined in the material file.  This is primarily intended for tools; 
        /// Mods only respect texture overrides that are definied in the mod .yaml file.
        ReadMaterialFile: bool
        /// Whether to reverse snapshot transforms on load.  Since meshes in tool-space
        /// are generally useless in game, this usually always
        /// happens.  It can be useful to turn it off in tools, so that the mod is displayed in post-snapshot 
        /// space (the same view that the tool will see).  The launcher preview window, for instance, turns this off.
        ReverseTransform: bool
    }

    /// Default read flags; used when no overriding flags are specified.
    let DefaultReadFlags = {
        ReadMaterialFile = false 
        ReverseTransform = true
    }

    /// Basic storage for everything that we consider to be "mesh data".  This is intentionally pretty close to the 
    /// renderer level; i.e. we don't have fields like "NormalMap" because the texture stage used for will vary
    /// across games or even within the same game.  Generally if you want to customize a texture its up to you to make
    /// sure its installed on the correct stage.
    type Mesh = {
        /// The type of the Mesh
        Type : ModType
        /// Array of indexed triangles; the indexes are for the Positions, UVs and Normals fields.
        Triangles : IndexedTri[]
        /// Array of positions.
        Positions: Vec3F[]
        /// Array of primary texture coordinate.  At the moment only one set of UVs is supported.
        UVs: Vec2F[]
        /// Array of normals.  Assumed to be normalized (by the 3D tool hopefully).
        Normals: Vec3F[]
        /// Array of blend indices.
        BlendIndices: Vec4X[]
        /// Array of blend weights.
        BlendWeights: Vec4F[]
        /// Vertex declaration; though this is optional, it is required for anything that will be displayed.
        Declaration : VertexDeclarationData option
        /// BinaryVertexData is usually not used.
        BinaryVertexData: BinaryVertexData option
        /// List of custom vertex group names, for use with vertex group inclusions/exclusions.
        AnnotatedVertexGroups: string list []
        /// List of position transforms that have been applied to this mesh.  Derived from snapshot transform profile.
        AppliedPositionTransforms: string []
        /// List of uv transforms that have been applied.  Derived from snapshot transform profile.
        AppliedUVTransforms: string[]
        /// Texture 0 path.  Generally only set if an override texture is being used, though the mesh read flags can affect this.
        Tex0Path: string 
        /// Texture 1 path.  Generally only set if an override texture is being used, though the mesh read flags can affect this.
        Tex1Path: string 
        /// Texture 2 path.  Generally only set if an override texture is being used, though the mesh read flags can affect this.
        Tex2Path: string 
        /// Texture 3 path.  Generally only set if an override texture is being used, though the mesh read flags can affect this.
        Tex3Path: string 
    }

    // ------------------------------------------------------------------------
    // These are types loaded by the moddb from yaml files

    /// Storage for a named Reference object.
    /// The Name of a reference is its base file name (no extension).
    type DBReference = {
        Name : string
        Mesh : Mesh
        PrimCount: int
        VertCount: int
    }

    /// Storage for a Deletion mod.
    type GeomDeletion = { PrimCount: int; VertCount: int }

    /// Other than base data, this contains additional data that can be set by a mod in the yaml file.
    type ModAttributes = {
        DeletedGeometry: GeomDeletion list
    }

    /// Default value
    let EmptyModAttributes = { ModAttributes.DeletedGeometry = [] }

    /// Storage for a named mod.
    /// The Name of a mod is its base file name (no extension).
    type DBMod = {
        RefName: string option
        Ref: DBReference option
        Name: string
        Mesh: Mesh option
        WeightMode: WeightMode
        PixelShader: string
        Attributes: ModAttributes
    }

    /// Union Parent type for the yaml objects.
    type ModElement = 
        Unknown 
        | MReference of DBReference
        | Mod of DBMod

