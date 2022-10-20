﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.IMGUI.Controls;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace net.fushizen.modular_avatar.core.editor
{
    public class BlendshapeSelectWindow : EditorWindow
    {
        internal GameObject AvatarRoot;
        private BlendshapeTree _tree;

        internal Action<BlendshapeBinding> OfferBinding;

        private void Awake()
        {
            titleContent = new GUIContent("Select blendshapes");
        }

        void OnGUI()
        {
            if (_tree == null)
            {
                _tree = new BlendshapeTree(AvatarRoot, new TreeViewState());
                _tree.OfferBinding = (binding) => OfferBinding?.Invoke(binding);
                _tree.Reload();

                _tree.SetExpanded(0, true);
            }

            _tree.OnGUI(new Rect(0, 0, position.width, position.height));
        }
    }

    internal class BlendshapeTree : TreeView
    {
        private readonly GameObject _avatarRoot;
        private List<BlendshapeBinding?> _candidateBindings;

        internal Action<BlendshapeBinding> OfferBinding;

        public BlendshapeTree(GameObject avatarRoot, TreeViewState state) : base(state)
        {
            this._avatarRoot = avatarRoot;
        }

        public BlendshapeTree(GameObject avatarRoot, TreeViewState state, MultiColumnHeader multiColumnHeader) : base(
            state, multiColumnHeader)
        {
            this._avatarRoot = avatarRoot;
        }

        protected override void DoubleClickedItem(int id)
        {
            var binding = _candidateBindings[id];
            if (binding.HasValue)
            {
                OfferBinding.Invoke(binding.Value);
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
            _candidateBindings = new List<BlendshapeBinding?>();
            _candidateBindings.Add(null);

            var allItems = new List<TreeViewItem>();

            int createdDepth = 0;
            List<string> ObjectDisplayNames = new List<string>();

            WalkTree(_avatarRoot, allItems, ObjectDisplayNames, ref createdDepth);

            SetupParentsAndChildrenFromDepths(root, allItems);

            return root;
        }

        private void WalkTree(GameObject node, List<TreeViewItem> items, List<string> objectDisplayNames,
            ref int createdDepth)
        {
            objectDisplayNames.Add(node.name);

            var smr = node.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null && smr.sharedMesh.blendShapeCount > 0)
            {
                while (createdDepth < objectDisplayNames.Count)
                {
                    items.Add(new TreeViewItem
                    {
                        id = _candidateBindings.Count, depth = createdDepth,
                        displayName = objectDisplayNames[createdDepth]
                    });
                    _candidateBindings.Add(null);
                    createdDepth++;
                }

                CreateBlendshapes(smr, items, ref createdDepth);
            }

            foreach (Transform child in node.transform)
            {
                WalkTree(child.gameObject, items, objectDisplayNames, ref createdDepth);
            }

            objectDisplayNames.RemoveAt(objectDisplayNames.Count - 1);
            createdDepth = Math.Min(createdDepth, objectDisplayNames.Count);
        }

        private void CreateBlendshapes(SkinnedMeshRenderer smr, List<TreeViewItem> items, ref int createdDepth)
        {
            items.Add(new TreeViewItem
                {id = _candidateBindings.Count, depth = createdDepth, displayName = "BlendShapes"});
            _candidateBindings.Add(null);
            createdDepth++;

            var path = RuntimeUtil.RelativePath(_avatarRoot, smr.gameObject);
            var mesh = smr.sharedMesh;
            List<BlendshapeBinding> bindings = Enumerable.Range(0, mesh.blendShapeCount)
                .Select(n =>
                {
                    var name = mesh.GetBlendShapeName(n);
                    return new BlendshapeBinding()
                    {
                        Blendshape = name,
                        ReferenceMesh = new AvatarObjectReference()
                        {
                            referencePath = path
                        }
                    };
                })
                .ToList();

            foreach (var binding in bindings)
            {
                items.Add(new TreeViewItem
                    {id = _candidateBindings.Count, depth = createdDepth, displayName = binding.Blendshape});
                _candidateBindings.Add(binding);
            }

            createdDepth--;
        }
    }
}