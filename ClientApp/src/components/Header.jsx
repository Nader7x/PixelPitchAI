import React from 'react';
import { Layout, Menu, Space } from 'antd';
import { Link, useLocation } from 'react-router-dom';
import { HomeOutlined, TeamOutlined, UserOutlined, ScheduleOutlined } from '@ant-design/icons';
import logo from '../assets/logo.png';
import SearchBar from './SearchBar';

const { Header: AntHeader } = Layout;

const Header = () => {
  const location = useLocation();
  const currentPath = location.pathname;

  return (
    <AntHeader className="header">
      <div className="logo-container">
        <Link to="/">
          <img src={logo} alt="Footex Logo" className="logo" />
        </Link>
      </div>
      <Menu 
        theme="dark" 
        mode="horizontal" 
        selectedKeys={[currentPath]}
        className="main-menu"
      >
        <Menu.Item key="/" icon={<HomeOutlined />}>
          <Link to="/">Home</Link>
        </Menu.Item>
        <Menu.Item key="/teams" icon={<TeamOutlined />}>
          <Link to="/teams">Teams</Link>
        </Menu.Item>
        <Menu.Item key="/players" icon={<UserOutlined />}>
          <Link to="/players">Players</Link>
        </Menu.Item>
        <Menu.Item key="/matches" icon={<ScheduleOutlined />}>
          <Link to="/matches">Matches</Link>
        </Menu.Item>
      </Menu>
      <div className="search-container">
        <SearchBar />
      </div>
    </AntHeader>
  );
};

export default Header;
