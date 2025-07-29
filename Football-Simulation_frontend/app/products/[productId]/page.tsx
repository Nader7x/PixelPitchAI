'use client';
import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import {
  ArrowLeft,
  Calendar,
  Star,
  Heart,
  ShoppingCart,
  Plus,
  Minus,
  Check,
  Truck,
  Shield,
  RotateCcw,
  Users,
  Home,
  LayoutDashboardIcon,
  Bell,
  User,
  Package,
  Settings,
  Search,
  ClubIcon,
  Tag,
  Info,
} from 'lucide-react';
import { SidebarLayout } from '../../Components/Sidebar/Sidebar';
import { SidebarItem } from '../../Components/Sidebar/SidebarItem';
import { SidebarSection } from '../../Components/Sidebar/SidebarSection';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import authService from '@/Services/AuthenticationService';

interface Product {
  id: string;
  name: string;
  description: string;
  price: number;
  originalPrice?: number;
  category: string;
  brand: string;
  rating: number;
  reviewCount: number;
  images: string[];
  inStock: boolean;
  stockQuantity: number;
  sizes?: string[];
  colors?: string[];
  features: string[];
  specifications: { [key: string]: string };
}

interface Review {
  id: string;
  user: string;
  rating: number;
  comment: string;
  date: string;
  verified: boolean;
}

export default function ProductDetailPage() {
  const params = useParams();
  const productId = params.productId as string;
  const [product, setProduct] = useState<Product | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isAdmin, setIsAdmin] = useState(false);
  const [quantity, setQuantity] = useState(1);
  const [selectedSize, setSelectedSize] = useState<string>('');
  const [selectedColor, setSelectedColor] = useState<string>('');
  const [selectedImage, setSelectedImage] = useState(0);
  const [isFavorite, setIsFavorite] = useState(false);
  const [cartItems, setCartItems] = useState<{ [key: string]: number }>({});
  const router = useRouter();

  useEffect(() => {
    if (!authService.isAuthenticated()) {
      router.push('/login');
      return;
    }

    setIsAdmin(authService.hasRole('Admin'));
    fetchProductDetails();
    loadCartItems();
    loadFavorites();
  }, [router, productId]);

  const fetchProductDetails = async () => {
    try {
      setLoading(true);
      setError(null);

      // Mock product data - replace with actual API call
      const mockProduct: Product = {
        id: productId,
        name: 'Professional Football Boots',
        description:
          'High-performance football boots designed for professional players. Features advanced technology for superior ball control, comfort, and durability on all playing surfaces.',
        price: 199.99,
        originalPrice: 249.99,
        category: 'Footwear',
        brand: 'SportTech Pro',
        rating: 4.7,
        reviewCount: 156,
        images: [
          '/images/Stadium dark.png', // Placeholder - replace with actual product images
          '/images/Stadium dark.png',
          '/images/Stadium dark.png',
        ],
        inStock: true,
        stockQuantity: 25,
        sizes: ['6', '7', '8', '9', '10', '11', '12'],
        colors: ['Black', 'White', 'Red', 'Blue'],
        features: [
          'Professional-grade synthetic upper',
          'Advanced stud configuration',
          'Lightweight design',
          'Enhanced ball control',
          'All-weather performance',
          'Ergonomic fit',
        ],
        specifications: {
          Material: 'Synthetic leather with mesh panels',
          Weight: '280g',
          Surface: 'Firm Ground (FG)',
          Closure: 'Lace-up',
          Warranty: '1 year manufacturer warranty',
        },
      };

      setProduct(mockProduct);
      if (mockProduct.sizes && mockProduct.sizes.length > 0) {
        setSelectedSize(mockProduct.sizes[0]);
      }
      if (mockProduct.colors && mockProduct.colors.length > 0) {
        setSelectedColor(mockProduct.colors[0]);
      }
    } catch (err) {
      console.error('Error fetching product details:', err);
      setError('Failed to load product details. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const loadCartItems = () => {
    const saved = localStorage.getItem('cart');
    if (saved) {
      setCartItems(JSON.parse(saved));
    }
  };

  const loadFavorites = () => {
    const saved = localStorage.getItem('favorites');
    if (saved) {
      const favorites = JSON.parse(saved);
      setIsFavorite(favorites.includes(productId));
    }
  };

  const updateCartItems = (newCart: { [key: string]: number }) => {
    setCartItems(newCart);
    localStorage.setItem('cart', JSON.stringify(newCart));
  };

  const addToCart = () => {
    if (!product) return;

    const newCart = { ...cartItems };
    const key = `${productId}-${selectedSize}-${selectedColor}`;
    newCart[key] = (newCart[key] || 0) + quantity;
    updateCartItems(newCart);

    // Show success feedback
    alert('Product added to cart!');
  };

  const toggleFavorite = () => {
    const saved = localStorage.getItem('favorites');
    const favorites = saved ? JSON.parse(saved) : [];

    if (isFavorite) {
      const index = favorites.indexOf(productId);
      if (index > -1) favorites.splice(index, 1);
    } else {
      favorites.push(productId);
    }

    localStorage.setItem('favorites', JSON.stringify(favorites));
    setIsFavorite(!isFavorite);
  };

  const getReviews = (): Review[] => {
    return [
      {
        id: '1',
        user: 'John D.',
        rating: 5,
        comment:
          'Excellent boots! Great quality and comfort. Perfect for my weekly games.',
        date: '2024-01-15',
        verified: true,
      },
      {
        id: '2',
        user: 'Sarah M.',
        rating: 4,
        comment:
          'Good quality boots, very comfortable. Sizing runs a bit large.',
        date: '2024-01-10',
        verified: true,
      },
      {
        id: '3',
        user: 'Mike R.',
        rating: 5,
        comment: 'Professional quality at a great price. Highly recommend!',
        date: '2024-01-05',
        verified: false,
      },
    ];
  };

  if (loading) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<ProductDetailSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 flex flex-col items-center space-y-6">
              <div className="h-16 w-16 animate-spin rounded-full border-4 border-green-500 border-t-transparent"></div>
              <h2 className="text-2xl font-bold text-gray-800">
                Loading Product Details...
              </h2>
            </div>
          </div>
        </SidebarLayout>
      </ProtectedRoute>
    );
  }

  if (error || !product) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<ProductDetailSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 text-center">
              <div className="rounded-lg border-l-4 border-red-500 bg-white p-6 text-red-700 shadow-lg">
                <p className="mb-2 text-xl font-bold">Error</p>
                <p className="mb-4">{error || 'Product not found'}</p>
                <div className="space-x-4">
                  <button
                    onClick={fetchProductDetails}
                    className="rounded-lg bg-red-500 px-4 py-2 text-white shadow transition-colors hover:bg-red-600"
                  >
                    Try Again
                  </button>
                  <Link
                    href="/products"
                    className="rounded-lg bg-gray-500 px-4 py-2 text-white shadow transition-colors hover:bg-gray-600"
                  >
                    Back to Products
                  </Link>
                </div>
              </div>
            </div>
          </div>
        </SidebarLayout>
      </ProtectedRoute>
    );
  }

  const reviews = getReviews();

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <SidebarLayout sidebar={<ProductDetailSidebar isAdmin={isAdmin} />}>
        <div className="relative min-h-screen">
          <BackgroundElements />

          <div className="relative z-10 p-6">
            {/* Header with Back Button */}
            <div className="mb-6">
              <Link
                href="/products"
                className="inline-flex items-center space-x-2 text-gray-600 transition-colors hover:text-gray-800"
              >
                <ArrowLeft className="h-5 w-5" />
                <span>Back to Products</span>
              </Link>
            </div>

            {/* Product Details */}
            <div className="grid gap-8 lg:grid-cols-2">
              {/* Product Images */}
              <div className="space-y-4">
                <div className="aspect-square overflow-hidden rounded-xl bg-white/80 shadow-lg backdrop-blur-sm">
                  <Image
                    src={product.images[selectedImage]}
                    alt={product.name}
                    width={500}
                    height={500}
                    className="h-full w-full object-cover"
                  />
                </div>
                <div className="flex space-x-2">
                  {product.images.map((image, index) => (
                    <button
                      key={index}
                      onClick={() => setSelectedImage(index)}
                      className={`relative h-20 w-20 overflow-hidden rounded-lg ${
                        selectedImage === index ? 'ring-2 ring-blue-500' : ''
                      }`}
                    >
                      <Image
                        src={image}
                        alt={`${product.name} ${index + 1}`}
                        fill
                        className="object-cover"
                      />
                    </button>
                  ))}
                </div>
              </div>

              {/* Product Information */}
              <div className="space-y-6">
                <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                  <div className="space-y-4">
                    {/* Title and Rating */}
                    <div>
                      <h1 className="text-3xl font-bold text-gray-900">
                        {product.name}
                      </h1>
                      <p className="mt-1 text-lg text-gray-600">
                        {product.brand}
                      </p>
                      <div className="mt-2 flex items-center space-x-2">
                        <div className="flex items-center">
                          {[...Array(5)].map((_, i) => (
                            <Star
                              key={i}
                              className={`h-5 w-5 ${
                                i < Math.floor(product.rating)
                                  ? 'fill-current text-yellow-500'
                                  : 'text-gray-300'
                              }`}
                            />
                          ))}
                        </div>
                        <span className="text-sm text-gray-600">
                          {product.rating} ({product.reviewCount} reviews)
                        </span>
                      </div>
                    </div>

                    {/* Price */}
                    <div className="flex items-center space-x-3">
                      <span className="text-3xl font-bold text-gray-900">
                        ${product.price.toFixed(2)}
                      </span>
                      {product.originalPrice && (
                        <span className="text-xl text-gray-500 line-through">
                          ${product.originalPrice.toFixed(2)}
                        </span>
                      )}
                      {product.originalPrice && (
                        <span className="rounded-full bg-red-100 px-2 py-1 text-sm font-medium text-red-800">
                          {Math.round(
                            ((product.originalPrice - product.price) /
                              product.originalPrice) *
                              100
                          )}
                          % OFF
                        </span>
                      )}
                    </div>

                    {/* Stock Status */}
                    <div className="flex items-center space-x-2">
                      {product.inStock ? (
                        <>
                          <Check className="h-5 w-5 text-green-600" />
                          <span className="font-medium text-green-600">
                            In Stock ({product.stockQuantity} available)
                          </span>
                        </>
                      ) : (
                        <span className="font-medium text-red-600">
                          Out of Stock
                        </span>
                      )}
                    </div>

                    {/* Size Selection */}
                    {product.sizes && (
                      <div>
                        <label className="mb-2 block text-sm font-medium text-gray-700">
                          Size
                        </label>
                        <div className="flex flex-wrap gap-2">
                          {product.sizes.map((size) => (
                            <button
                              key={size}
                              onClick={() => setSelectedSize(size)}
                              className={`rounded-lg border px-4 py-2 text-sm font-medium transition-colors ${
                                selectedSize === size
                                  ? 'border-blue-500 bg-blue-50 text-blue-700'
                                  : 'border-gray-300 text-gray-700 hover:border-gray-400'
                              }`}
                            >
                              {size}
                            </button>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Color Selection */}
                    {product.colors && (
                      <div>
                        <label className="mb-2 block text-sm font-medium text-gray-700">
                          Color
                        </label>
                        <div className="flex flex-wrap gap-2">
                          {product.colors.map((color) => (
                            <button
                              key={color}
                              onClick={() => setSelectedColor(color)}
                              className={`rounded-lg border px-4 py-2 text-sm font-medium transition-colors ${
                                selectedColor === color
                                  ? 'border-blue-500 bg-blue-50 text-blue-700'
                                  : 'border-gray-300 text-gray-700 hover:border-gray-400'
                              }`}
                            >
                              {color}
                            </button>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Quantity */}
                    <div>
                      <label className="mb-2 block text-sm font-medium text-gray-700">
                        Quantity
                      </label>
                      <div className="flex items-center space-x-2">
                        <button
                          onClick={() => setQuantity(Math.max(1, quantity - 1))}
                          className="rounded-lg border border-gray-300 p-2 hover:bg-gray-50"
                        >
                          <Minus className="h-4 w-4" />
                        </button>
                        <span className="min-w-[60px] rounded-lg border border-gray-300 px-4 py-2 text-center">
                          {quantity}
                        </span>
                        <button
                          onClick={() => setQuantity(quantity + 1)}
                          className="rounded-lg border border-gray-300 p-2 hover:bg-gray-50"
                        >
                          <Plus className="h-4 w-4" />
                        </button>
                      </div>
                    </div>

                    {/* Action Buttons */}
                    <div className="flex space-x-4">
                      <button
                        onClick={addToCart}
                        disabled={!product.inStock}
                        className="flex flex-1 items-center justify-center space-x-2 rounded-lg bg-blue-600 px-6 py-3 font-medium text-white transition-colors hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-gray-400"
                      >
                        <ShoppingCart className="h-5 w-5" />
                        <span>Add to Cart</span>
                      </button>
                      <button
                        onClick={toggleFavorite}
                        className={`rounded-lg border p-3 transition-colors ${
                          isFavorite
                            ? 'border-red-500 bg-red-50 text-red-600'
                            : 'border-gray-300 text-gray-600 hover:border-gray-400'
                        }`}
                      >
                        <Heart
                          className={`h-5 w-5 ${isFavorite ? 'fill-current' : ''}`}
                        />
                      </button>
                    </div>
                  </div>
                </div>

                {/* Shipping & Returns */}
                <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                  <h3 className="mb-4 text-lg font-semibold text-gray-900">
                    Shipping & Returns
                  </h3>
                  <div className="space-y-3">
                    <div className="flex items-center space-x-3">
                      <Truck className="h-5 w-5 text-green-600" />
                      <span className="text-gray-700">
                        Free shipping on orders over $100
                      </span>
                    </div>
                    <div className="flex items-center space-x-3">
                      <RotateCcw className="h-5 w-5 text-blue-600" />
                      <span className="text-gray-700">
                        30-day return policy
                      </span>
                    </div>
                    <div className="flex items-center space-x-3">
                      <Shield className="h-5 w-5 text-purple-600" />
                      <span className="text-gray-700">
                        1-year manufacturer warranty
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Product Details Tabs */}
            <div className="mt-12 space-y-8">
              {/* Description */}
              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <h2 className="mb-4 text-xl font-bold text-gray-900">
                  Description
                </h2>
                <p className="leading-relaxed text-gray-700">
                  {product.description}
                </p>
              </div>

              {/* Features & Specifications */}
              <div className="grid gap-8 lg:grid-cols-2">
                <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                  <h3 className="mb-4 text-lg font-semibold text-gray-900">
                    Features
                  </h3>
                  <ul className="space-y-2">
                    {product.features.map((feature, index) => (
                      <li key={index} className="flex items-center space-x-3">
                        <Check className="h-4 w-4 flex-shrink-0 text-green-600" />
                        <span className="text-gray-700">{feature}</span>
                      </li>
                    ))}
                  </ul>
                </div>

                <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                  <h3 className="mb-4 text-lg font-semibold text-gray-900">
                    Specifications
                  </h3>
                  <dl className="space-y-3">
                    {Object.entries(product.specifications).map(
                      ([key, value]) => (
                        <div key={key} className="flex justify-between">
                          <dt className="text-gray-600">{key}:</dt>
                          <dd className="font-medium text-gray-900">{value}</dd>
                        </div>
                      )
                    )}
                  </dl>
                </div>
              </div>

              {/* Reviews */}
              <div className="rounded-xl bg-white/80 p-6 shadow-lg backdrop-blur-sm">
                <h3 className="mb-4 text-lg font-semibold text-gray-900">
                  Customer Reviews ({reviews.length})
                </h3>
                <div className="space-y-6">
                  {reviews.map((review) => (
                    <div
                      key={review.id}
                      className="border-b border-gray-200 pb-6 last:border-b-0"
                    >
                      <div className="mb-2 flex items-start justify-between">
                        <div className="flex items-center space-x-2">
                          <span className="font-medium text-gray-900">
                            {review.user}
                          </span>
                          {review.verified && (
                            <span className="rounded-full bg-green-100 px-2 py-1 text-xs text-green-800">
                              Verified Purchase
                            </span>
                          )}
                        </div>
                        <span className="text-sm text-gray-500">
                          {new Date(review.date).toLocaleDateString()}
                        </span>
                      </div>
                      <div className="mb-2 flex items-center">
                        {[...Array(5)].map((_, i) => (
                          <Star
                            key={i}
                            className={`h-4 w-4 ${
                              i < review.rating
                                ? 'fill-current text-yellow-500'
                                : 'text-gray-300'
                            }`}
                          />
                        ))}
                      </div>
                      <p className="text-gray-700">{review.comment}</p>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
      </SidebarLayout>
    </ProtectedRoute>
  );
}

function ProductDetailSidebar({ isAdmin }: { isAdmin: boolean }) {
  return (
    <>
      <Link href="/dashboard">
        <SidebarItem
          icon={<LayoutDashboardIcon size={20} />}
          text="Dashboard"
        />
      </Link>{' '}
      <Link href="/teams">
        <SidebarItem icon={<ClubIcon size={20} />} text="Teams" />
      </Link>
      <Link href="/players">
        <SidebarItem icon={<User size={20} />} text="Players" />
      </Link>
      <Link href="/coaches">
        <SidebarItem icon={<Users size={20} />} text="Coaches" />
      </Link>
      <Link href="/stadiums">
        <SidebarItem icon={<Home size={20} />} text="Stadiums" />
      </Link>
      {isAdmin && (
        <>
          <SidebarSection title="Admin" color="text-amber-600" />
          <Link href="/admin">
            <SidebarItem icon={<Settings size={20} />} text="Admin Dashboard" />
          </Link>
        </>
      )}
      <Link href="/notifications">
        <SidebarItem icon={<Bell size={20} />} text="Notifications" />
      </Link>
      <SidebarSection title="Other" />
      <Link href="/search">
        <SidebarItem icon={<Search size={20} />} text="Search" />
      </Link>
      <Link href="/settings">
        <SidebarItem icon={<Settings size={20} />} text="Settings" />
      </Link>
    </>
  );
}

function BackgroundElements() {
  return (
    <div className="fixed inset-0 z-0">
      <div className="absolute inset-0 bg-gradient-to-br from-blue-50 via-purple-50 to-indigo-50"></div>

      <div className="absolute inset-0 opacity-[0.03]">
        <Image
          src="/images/Stadium dark.png"
          alt="Stadium Background"
          fill
          className="object-cover object-center"
        />
      </div>

      <div className="absolute inset-0 overflow-hidden">
        <div className="animate-float absolute top-20 left-20 h-2 w-2 rounded-full bg-blue-400/20"></div>
        <div className="animate-float-delayed absolute top-40 right-32 h-3 w-3 rounded-full bg-purple-400/15"></div>
        <div className="animate-float-slow absolute bottom-32 left-40 h-1 w-1 rounded-full bg-indigo-400/25"></div>
        <div className="animate-float-delayed absolute right-20 bottom-20 h-2 w-2 rounded-full bg-blue-300/20"></div>
      </div>
    </div>
  );
}
