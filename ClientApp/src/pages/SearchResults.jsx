import React, { useState, useEffect } from 'react';
import { useLocation, useNavigate, Link } from 'react-router-dom';
import { List, Card, Avatar, Pagination, Empty, Spin, Typography, Tag } from 'antd';
import { TeamOutlined, UserOutlined, TrophyOutlined, CalendarOutlined } from '@ant-design/icons';
import searchService from '../services/searchService';

const { Title, Text, Paragraph } = Typography;

const SearchResults = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const searchParams = new URLSearchParams(location.search);
  const query = searchParams.get('q') || '';
  const pageParam = parseInt(searchParams.get('page')) || 1;
  
  const [results, setResults] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [currentPage, setCurrentPage] = useState(pageParam);
  const [pageSize] = useState(10);

  useEffect(() => {
    if (query.length >= 2) {
      fetchResults();
    } else if (query.length === 0) {
      setResults({ items: [], totalResults: 0, totalPages: 0 });
    }
  }, [query, currentPage, pageSize]);

  const fetchResults = async () => {
    setLoading(true);
    setError(null);
    
    try {
      const data = await searchService.search(query, currentPage, pageSize);
      setResults(data);
    } catch (err) {
      setError('An error occurred while searching. Please try again later.');
      console.error('Search error:', err);
    } finally {
      setLoading(false);
    }
  };

  const handlePageChange = (page) => {
    setCurrentPage(page);
    searchParams.set('page', page.toString());
    navigate({ search: searchParams.toString() });
  };

  const getIconForType = (type) => {
    switch (type) {
      case 'Team':
        return <TeamOutlined />;
      case 'Player':
        return <UserOutlined />;
      case 'Coach':
        return <UserOutlined />;
      case 'Match':
        return <CalendarOutlined />;
      default:
        return <TrophyOutlined />;
    }
  };

  const getTagColorForType = (type) => {
    switch (type) {
      case 'Team':
        return 'blue';
      case 'Player':
        return 'green';
      case 'Coach':
        return 'purple';
      case 'Match':
        return 'orange';
      default:
        return 'default';
    }
  };

  return (
    <div className="search-results-container">
      <Title level={2}>Search Results for "{query}"</Title>
      
      {loading ? (
        <div className="loading-container">
          <Spin size="large" />
          <p>Searching...</p>
        </div>
      ) : error ? (
        <div className="error-container">
          <Text type="danger">{error}</Text>
        </div>
      ) : results && results.items.length === 0 ? (
        <Empty 
          description={
            <span>
              No results found for <strong>"{query}"</strong>
            </span>
          } 
        />
      ) : results && (
        <>
          <Text type="secondary">
            Found {results.totalResults} results
          </Text>
          
          <List
            grid={{ gutter: 16, xs: 1, sm: 1, md: 2, lg: 2, xl: 3, xxl: 4 }}
            dataSource={results.items}
            renderItem={item => (
              <List.Item>
                <Link to={item.url}>
                  <Card 
                    hoverable
                    className="search-result-card"
                    cover={item.thumbnailUrl && (
                      <div className="card-thumbnail">
                        <img alt={item.name} src={item.thumbnailUrl} />
                      </div>
                    )}
                  >
                    <Card.Meta
                      avatar={<Avatar icon={getIconForType(item.type)} />}
                      title={
                        <div>
                          {item.name}
                          <Tag color={getTagColorForType(item.type)} className="result-type-tag">
                            {item.type}
                          </Tag>
                        </div>
                      }
                      description={
                        <div>
                          <Paragraph ellipsis={{ rows: 2 }}>{item.description}</Paragraph>
                          {item.additionalData && Object.entries(item.additionalData).map(([key, value]) => (
                            <div key={key} className="additional-data-item">
                              <Text strong>{key}:</Text> {value}
                            </div>
                          ))}
                        </div>
                      }
                    />
                  </Card>
                </Link>
              </List.Item>
            )}
          />
          
          {results.totalPages > 1 && (
            <div className="pagination-container">
              <Pagination
                current={currentPage}
                total={results.totalResults}
                pageSize={pageSize}
                onChange={handlePageChange}
                showSizeChanger={false}
              />
            </div>
          )}
        </>
      )}
    </div>
  );
};

export default SearchResults;
