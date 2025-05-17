import axios from 'axios';

const API_URL = '/api/search';

export const searchService = {
  search: async (query, page = 1, pageSize = 10) => {
    try {
      const response = await axios.get(API_URL, {
        params: {
          query,
          page,
          pageSize
        }
      });
      return response.data;
    } catch (error) {
      console.error('Error searching:', error);
      throw error;
    }
  }
};

export default searchService;
