'use client';
import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import {
  Search,
  Filter,
  Package,
  ShoppingCart,
  Star,
  TrendingUp,
  Settings,
  ClubIcon,
  Bell,
  User,
  Users,
  Home,
  LayoutDashboardIcon,
  DollarSign,
  Tag,
  Calendar,
  Heart,
  Eye,
  Zap,
} from 'lucide-react';
import { SidebarLayout } from '../Components/Sidebar/Sidebar';
import { SidebarItem } from '../Components/Sidebar/SidebarItem';
import { SidebarSection } from '../Components/Sidebar/SidebarSection';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';
import authService from '@/Services/AuthenticationService';

interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  originalPrice?: number;
  imageUrl?: string;
  category: string;
  inStock: boolean;
  rating: number;
  reviewCount: number;
  isNew?: boolean;
  isFeatured?: boolean;
  discount?: number;
  teamId?: number;
  teamName?: string;
}

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [filteredProducts, setFilteredProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [sortBy, setSortBy] = useState('featured');
  const [isAdmin, setIsAdmin] = useState(false);
  const [cart, setCart] = useState<Product[]>([]);
  const [favorites, setFavorites] = useState<number[]>([]);
  const router = useRouter();

  useEffect(() => {
    if (!authService.isAuthenticated()) {
      router.push('/login');
      return;
    }

    setIsAdmin(authService.hasRole('Admin'));
    fetchProducts();
    loadCartFromStorage();
    loadFavoritesFromStorage();
  }, [router]);

  useEffect(() => {
    filterAndSortProducts();
  }, [products, searchQuery, categoryFilter, sortBy]);

  const fetchProducts = async () => {
    try {
      setLoading(true);
      // Mock product data - replace with actual API call
      const mockProducts: Product[] = [
        {
          id: 1,
          name: 'Official Team Jersey - Home',
          description:
            'High-quality official home jersey with moisture-wicking technology',
          price: 89.99,
          originalPrice: 109.99,
          imageUrl: '/products/jersey-home.jpg',
          category: 'Jerseys',
          inStock: true,
          rating: 4.8,
          reviewCount: 245,
          isNew: false,
          isFeatured: true,
          discount: 18,
          teamId: 1,
          teamName: 'Barcelona',
        },
        {
          id: 2,
          name: 'Football Training Ball',
          description:
            'Professional training ball approved for official matches',
          price: 29.99,
          imageUrl: '/products/football.jpg',
          category: 'Equipment',
          inStock: true,
          rating: 4.6,
          reviewCount: 128,
          isNew: true,
          isFeatured: false,
        },
        {
          id: 3,
          name: 'Team Scarf - Supporters Edition',
          description: 'Premium quality team scarf perfect for match days',
          price: 24.99,
          imageUrl: '/products/scarf.jpg',
          category: 'Accessories',
          inStock: true,
          rating: 4.5,
          reviewCount: 89,
          isNew: false,
          isFeatured: false,
          teamId: 1,
          teamName: 'Barcelona',
        },
        {
          id: 4,
          name: 'Premium Football Boots',
          description:
            'Professional-grade football boots with advanced grip technology',
          price: 149.99,
          originalPrice: 199.99,
          imageUrl: '/products/boots.jpg',
          category: 'Footwear',
          inStock: true,
          rating: 4.9,
          reviewCount: 67,
          isNew: true,
          isFeatured: true,
          discount: 25,
        },
        {
          id: 5,
          name: 'Team Cap - Classic Design',
          description: 'Comfortable team cap with adjustable strap',
          price: 19.99,
          imageUrl: '/products/cap.jpg',
          category: 'Accessories',
          inStock: false,
          rating: 4.3,
          reviewCount: 156,
          isNew: false,
          isFeatured: false,
          teamId: 2,
          teamName: 'Real Madrid',
        },
        {
          id: 6,
          name: 'Training Kit Set',
          description:
            'Complete training kit including shirt, shorts, and socks',
          price: 69.99,
          imageUrl: '/products/training-kit.jpg',
          category: 'Kits',
          inStock: true,
          rating: 4.7,
          reviewCount: 203,
          isNew: false,
          isFeatured: true,
        },
      ];

      setProducts(mockProducts);
    } catch (err) {
      console.error('Error fetching products:', err);
      setError('Failed to load products. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const filterAndSortProducts = () => {
    let filtered = [...products];

    // Filter by search query
    if (searchQuery.trim()) {
      filtered = filtered.filter(
        (product) =>
          product.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
          product.description
            .toLowerCase()
            .includes(searchQuery.toLowerCase()) ||
          product.category.toLowerCase().includes(searchQuery.toLowerCase()) ||
          product.teamName?.toLowerCase().includes(searchQuery.toLowerCase())
      );
    }

    // Filter by category
    if (categoryFilter && categoryFilter !== 'all') {
      filtered = filtered.filter(
        (product) =>
          product.category.toLowerCase() === categoryFilter.toLowerCase()
      );
    }

    // Sort products
    switch (sortBy) {
      case 'price-low':
        filtered.sort((a, b) => a.price - b.price);
        break;
      case 'price-high':
        filtered.sort((a, b) => b.price - a.price);
        break;
      case 'rating':
        filtered.sort((a, b) => b.rating - a.rating);
        break;
      case 'newest':
        filtered.sort((a, b) => (b.isNew ? 1 : 0) - (a.isNew ? 1 : 0));
        break;
      case 'featured':
      default:
        filtered.sort(
          (a, b) => (b.isFeatured ? 1 : 0) - (a.isFeatured ? 1 : 0)
        );
        break;
    }

    setFilteredProducts(filtered);
  };

  const loadCartFromStorage = () => {
    const savedCart = localStorage.getItem('footballShopCart');
    if (savedCart) {
      setCart(JSON.parse(savedCart));
    }
  };

  const loadFavoritesFromStorage = () => {
    const savedFavorites = localStorage.getItem('footballShopFavorites');
    if (savedFavorites) {
      setFavorites(JSON.parse(savedFavorites));
    }
  };

  const addToCart = (product: Product) => {
    const updatedCart = [...cart, product];
    setCart(updatedCart);
    localStorage.setItem('footballShopCart', JSON.stringify(updatedCart));
  };

  const toggleFavorite = (productId: number) => {
    const updatedFavorites = favorites.includes(productId)
      ? favorites.filter((id) => id !== productId)
      : [...favorites, productId];

    setFavorites(updatedFavorites);
    localStorage.setItem(
      'footballShopFavorites',
      JSON.stringify(updatedFavorites)
    );
  };

  const getCategories = () => {
    const categories = products
      .map((product) => product.category)
      .filter((value, index, self) => self.indexOf(value) === index)
      .sort();
    return categories;
  };

  const formatPrice = (price: number) => {
    return `$${price.toFixed(2)}`;
  };

  const renderStars = (rating: number) => {
    const stars = [];
    const fullStars = Math.floor(rating);
    const hasHalfStar = rating % 1 !== 0;

    for (let i = 0; i < fullStars; i++) {
      stars.push(
        <Star key={i} className="h-4 w-4 fill-yellow-400 text-yellow-400" />
      );
    }

    if (hasHalfStar) {
      stars.push(
        <Star
          key="half"
          className="h-4 w-4 fill-yellow-400/50 text-yellow-400"
        />
      );
    }

    const remainingStars = 5 - Math.ceil(rating);
    for (let i = 0; i < remainingStars; i++) {
      stars.push(<Star key={`empty-${i}`} className="h-4 w-4 text-gray-300" />);
    }

    return stars;
  };

  if (loading) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<ProductsSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 flex flex-col items-center space-y-6">
              <div className="h-16 w-16 animate-spin rounded-full border-4 border-green-500 border-t-transparent"></div>
              <h2 className="text-2xl font-bold text-gray-800">
                Loading Products...
              </h2>
            </div>
          </div>
        </SidebarLayout>
      </ProtectedRoute>
    );
  }

  if (error) {
    return (
      <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
        <SidebarLayout sidebar={<ProductsSidebar isAdmin={isAdmin} />}>
          <div className="relative flex h-screen items-center justify-center">
            <BackgroundElements />
            <div className="relative z-10 text-center">
              <div className="rounded-lg border-l-4 border-red-500 bg-white p-6 text-red-700 shadow-lg">
                <p className="mb-2 text-xl font-bold">Error</p>
                <p className="mb-4">{error}</p>
                <button
                  onClick={fetchProducts}
                  className="rounded-lg bg-red-500 px-4 py-2 text-white shadow transition-colors hover:bg-red-600"
                >
                  Try Again
                </button>
              </div>
            </div>
          </div>
        </SidebarLayout>
      </ProtectedRoute>
    );
  }

  return (
    <ProtectedRoute allowedRoles={['User', 'Admin', 'Coach', 'Player']}>
      <SidebarLayout sidebar={<ProductsSidebar isAdmin={isAdmin} />}>
        <div className="relative min-h-screen">
          <BackgroundElements />

          <div className="relative z-10 p-6">
            {/* Header Section */}
            <div className="mb-8">
              <div className="flex items-center justify-between">
                <div>
                  <h1 className="text-3xl font-bold text-gray-900">
                    Football Shop
                  </h1>
                  <p className="mt-2 text-gray-600">
                    Official merchandise and equipment for football enthusiasts
                  </p>
                </div>
                <div className="flex items-center space-x-4">
                  <div className="rounded-lg bg-white/80 p-4 shadow-lg backdrop-blur-sm">
                    <div className="flex items-center space-x-2">
                      <Package className="h-8 w-8 text-green-600" />
                      <div>
                        <p className="text-2xl font-bold text-gray-900">
                          {products.length}
                        </p>
                        <p className="text-sm text-gray-600">Products</p>
                      </div>
                    </div>
                  </div>
                  <div className="rounded-lg bg-white/80 p-4 shadow-lg backdrop-blur-sm">
                    <div className="flex items-center space-x-2">
                      <ShoppingCart className="h-8 w-8 text-blue-600" />
                      <div>
                        <p className="text-2xl font-bold text-gray-900">
                          {cart.length}
                        </p>
                        <p className="text-sm text-gray-600">In Cart</p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Search and Filters */}
            <div className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
              <div className="relative max-w-md flex-1">
                <input
                  type="text"
                  placeholder="Search products..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full rounded-lg border border-gray-200 bg-white/80 px-10 py-3 text-gray-700 placeholder-gray-500 backdrop-blur-sm focus:border-green-500 focus:ring-1 focus:ring-green-500 focus:outline-none"
                />
                <Search className="absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-gray-400" />
              </div>

              <div className="flex items-center space-x-4">
                <select
                  value={categoryFilter}
                  onChange={(e) => setCategoryFilter(e.target.value)}
                  className="rounded-lg border border-gray-200 bg-white/80 px-4 py-3 text-gray-700 backdrop-blur-sm focus:border-green-500 focus:ring-1 focus:ring-green-500 focus:outline-none"
                >
                  <option value="">All Categories</option>
                  {getCategories().map((category) => (
                    <option key={category} value={category}>
                      {category}
                    </option>
                  ))}
                </select>

                <select
                  value={sortBy}
                  onChange={(e) => setSortBy(e.target.value)}
                  className="rounded-lg border border-gray-200 bg-white/80 px-4 py-3 text-gray-700 backdrop-blur-sm focus:border-green-500 focus:ring-1 focus:ring-green-500 focus:outline-none"
                >
                  <option value="featured">Featured</option>
                  <option value="newest">Newest</option>
                  <option value="price-low">Price: Low to High</option>
                  <option value="price-high">Price: High to Low</option>
                  <option value="rating">Highest Rated</option>
                </select>

                <button className="flex items-center space-x-2 rounded-lg border border-gray-200 bg-white/80 px-4 py-3 text-gray-700 backdrop-blur-sm transition-colors hover:bg-gray-50">
                  <Filter className="h-4 w-4" />
                  <span>Filters</span>
                </button>
              </div>
            </div>

            {/* Products Grid */}
            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
              {filteredProducts.map((product) => (
                <div
                  key={product.id}
                  className="group transform transition-all duration-200 hover:scale-105"
                >
                  <div className="overflow-hidden rounded-xl bg-white/80 shadow-lg backdrop-blur-sm transition-all hover:shadow-xl">
                    {/* Product Image */}
                    <div className="relative h-48 w-full bg-gradient-to-br from-gray-200 to-gray-300">
                      {product.imageUrl ? (
                        <Image
                          src={product.imageUrl}
                          alt={product.name}
                          fill
                          className="object-cover transition-transform duration-300 group-hover:scale-105"
                        />
                      ) : (
                        <div className="flex h-full w-full items-center justify-center">
                          <Package className="h-16 w-16 text-gray-400" />
                        </div>
                      )}

                      {/* Badges */}
                      <div className="absolute top-3 left-3 space-y-2">
                        {product.isNew && (
                          <span className="rounded-full bg-green-500 px-2 py-1 text-xs font-bold text-white">
                            NEW
                          </span>
                        )}
                        {product.isFeatured && (
                          <span className="rounded-full bg-blue-500 px-2 py-1 text-xs font-bold text-white">
                            FEATURED
                          </span>
                        )}
                        {product.discount && (
                          <span className="rounded-full bg-red-500 px-2 py-1 text-xs font-bold text-white">
                            -{product.discount}%
                          </span>
                        )}
                      </div>

                      {/* Stock Status */}
                      {!product.inStock && (
                        <div className="absolute inset-0 flex items-center justify-center bg-black/50">
                          <span className="rounded-full bg-red-500 px-3 py-1 text-sm font-bold text-white">
                            OUT OF STOCK
                          </span>
                        </div>
                      )}

                      {/* Favorite Button */}
                      <button
                        onClick={() => toggleFavorite(product.id)}
                        className="absolute top-3 right-3 rounded-full bg-white/90 p-2 shadow-md transition-colors hover:bg-white"
                      >
                        <Heart
                          className={`h-4 w-4 ${
                            favorites.includes(product.id)
                              ? 'fill-red-500 text-red-500'
                              : 'text-gray-600'
                          }`}
                        />
                      </button>
                    </div>

                    {/* Product Info */}
                    <div className="p-4">
                      <div className="mb-2">
                        <span className="text-xs tracking-wide text-gray-500 uppercase">
                          {product.category}
                        </span>
                        {product.teamName && (
                          <span className="ml-2 text-xs text-blue-600">
                            • {product.teamName}
                          </span>
                        )}
                      </div>

                      <h3 className="mb-2 font-bold text-gray-900 transition-colors group-hover:text-green-600">
                        {product.name}
                      </h3>

                      <p className="mb-3 line-clamp-2 text-sm text-gray-600">
                        {product.description}
                      </p>

                      {/* Rating */}
                      <div className="mb-3 flex items-center space-x-2">
                        <div className="flex items-center space-x-1">
                          {renderStars(product.rating)}
                        </div>
                        <span className="text-sm text-gray-600">
                          {product.rating} ({product.reviewCount})
                        </span>
                      </div>

                      {/* Price */}
                      <div className="mb-4 flex items-center justify-between">
                        <div className="flex items-center space-x-2">
                          <span className="text-xl font-bold text-gray-900">
                            {formatPrice(product.price)}
                          </span>
                          {product.originalPrice && (
                            <span className="text-sm text-gray-500 line-through">
                              {formatPrice(product.originalPrice)}
                            </span>
                          )}
                        </div>
                      </div>

                      {/* Action Buttons */}
                      <div className="flex space-x-2">
                        <Link
                          href={`/products/${product.id}`}
                          className="flex flex-1 items-center justify-center space-x-2 rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-50"
                        >
                          <Eye className="h-4 w-4" />
                          <span>View</span>
                        </Link>
                        <button
                          onClick={() => addToCart(product)}
                          disabled={!product.inStock}
                          className={`flex flex-1 items-center justify-center space-x-2 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
                            product.inStock
                              ? 'bg-green-600 text-white hover:bg-green-700'
                              : 'cursor-not-allowed bg-gray-300 text-gray-500'
                          }`}
                        >
                          <ShoppingCart className="h-4 w-4" />
                          <span>
                            {product.inStock ? 'Add to Cart' : 'Out of Stock'}
                          </span>
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>

            {/* Empty State */}
            {filteredProducts.length === 0 && !loading && (
              <div className="py-12 text-center">
                <Package className="mx-auto mb-4 h-16 w-16 text-gray-400" />
                <h3 className="mb-2 text-lg font-medium text-gray-900">
                  No products found
                </h3>
                <p className="text-gray-600">
                  {searchQuery || categoryFilter
                    ? 'Try adjusting your search or filters.'
                    : 'No products are available at the moment.'}
                </p>
              </div>
            )}
          </div>
        </div>
      </SidebarLayout>
    </ProtectedRoute>
  );
}

function ProductsSidebar({ isAdmin }: { isAdmin: boolean }) {
  return (
    <>
      {/* Main Navigation */}
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
      {/* Admin Section */}
      {isAdmin && (
        <>
          <SidebarSection title="Admin" color="text-amber-600" />
          <Link href="/admin">
            <SidebarItem icon={<Settings size={20} />} text="Admin Dashboard" />
          </Link>
        </>
      )}{' '}
      {/* Notifications */}
      <Link href="/notifications">
        <SidebarItem icon={<Bell size={20} />} text="Notifications" />
      </Link>
      {/* Search & Settings */}
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
    <div className="absolute inset-0 overflow-hidden">
      {/* Gradient Background */}
      <div className="absolute inset-0 bg-gradient-to-br from-green-50 via-white to-blue-50"></div>

      {/* Floating Elements */}
      <div className="absolute top-20 left-20 h-4 w-4 animate-ping rounded-full bg-green-300/30"></div>
      <div className="absolute top-40 right-32 h-6 w-6 animate-pulse rounded-full bg-blue-300/20"></div>
      <div className="absolute bottom-32 left-40 h-5 w-5 animate-bounce rounded-full bg-green-400/25"></div>
      <div className="absolute right-20 bottom-20 h-3 w-3 animate-pulse rounded-full bg-blue-400/30"></div>

      {/* Decorative Shapes */}
      <div className="absolute top-1/4 right-10 h-32 w-32 rounded-full bg-gradient-to-br from-green-100/30 to-blue-100/30 blur-3xl"></div>
      <div className="absolute bottom-1/4 left-10 h-40 w-40 rounded-full bg-gradient-to-br from-blue-100/20 to-green-100/20 blur-3xl"></div>
    </div>
  );
}
