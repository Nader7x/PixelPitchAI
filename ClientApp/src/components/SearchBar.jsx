import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Input, Button, Form } from 'antd';
import { SearchOutlined } from '@ant-design/icons';

const SearchBar = () => {
  const [query, setQuery] = useState('');
  const navigate = useNavigate();

  const handleSearch = (e) => {
    e.preventDefault();
    if (query.trim().length >= 2) {
      navigate(`/search?q=${encodeURIComponent(query.trim())}`);
    }
  };

  return (
    <Form onSubmit={handleSearch} className="search-form">
      <Input 
        placeholder="Search teams, players, matches..." 
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        prefix={<SearchOutlined />}
        onPressEnter={handleSearch}
      />
      <Button 
        type="primary" 
        icon={<SearchOutlined />} 
        onClick={handleSearch}
        disabled={query.trim().length < 2}
      >
        Search
      </Button>
    </Form>
  );
};

export default SearchBar;
