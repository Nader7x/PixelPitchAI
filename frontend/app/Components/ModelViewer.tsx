'use client';

import { Canvas, useLoader } from '@react-three/fiber';
import { OrbitControls } from '@react-three/drei';
import { Suspense } from 'react';
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-expect-error
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader';

function Model() {
  const gltf = useLoader(GLTFLoader, 'models/football2.glb');
  return <primitive object={gltf.scene} scale={0.6} position={[0, -1, 0]} />;
}

// function Model() {
//     const fbx = useLoader(FBXLoader, 'models/football_lp.fbx');
//     return <primitive object={fbx} scale={0.6} position={[0, -1, 0]} />;
// }

export default function ModelViewer() {
  return (
    <div className="h-[500px] w-full">
      {' '}
      {/* Adjust height if needed */}
      <Canvas camera={{ position: [0, 0, 3], fov: 45 }}>
        <ambientLight intensity={0.5} />
        <directionalLight position={[5, 5, 5]} intensity={1.5} />
        <pointLight position={[0, 2, 2]} intensity={0.8} />

        <Suspense fallback={null}>
          <Model />
        </Suspense>

        <OrbitControls
          target={[0, -1, 0]}
          enableZoom={true}
          autoRotate
          autoRotateSpeed={2.5}
        />
      </Canvas>
    </div>
  );
}
