'use client';
import { useEffect } from 'react';
import { useRouter } from 'next/navigation';

import Hero from '@/app/home/page';

export default function Home() {
  const router = useRouter();

  useEffect(() => {
    router.push('/home');
  }, [router]);

  return (
    <div>
      <Hero></Hero>
    </div>
  );
}
