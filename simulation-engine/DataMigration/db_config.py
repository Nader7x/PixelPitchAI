# Database configuration file
# Update these settings with your actual database credentials

# Source database configuration (Footex - Training Database)
SOURCE_DB_CONFIG = {
    'host': 'localhost',  # Your PostgreSQL server host
    'database': 'Footex',
    'user': 'postgres',  # Replace with your PostgreSQL username
    'password': '0000',  # Replace with your PostgreSQL password
    'port': 5432  # Default PostgreSQL port
}

# Target database configuration (Footex_Api - API Database)
TARGET_DB_CONFIG = {
    'host': 'localhost',  # Your PostgreSQL server host  
    'database': 'Footex_Api',
    'user': 'postgres',  # Replace with your PostgreSQL username
    'password': '0000',  # Replace with your PostgreSQL password
    'port': 5432  # Default PostgreSQL port
}

# Alternative: Use environment variables for security
# import os
# 
# SOURCE_DB_CONFIG = {
#     'host': os.getenv('SOURCE_DB_HOST', 'localhost'),
#     'database': os.getenv('SOURCE_DB_NAME', 'Footex'),
#     'user': os.getenv('SOURCE_DB_USER'),
#     'password': os.getenv('SOURCE_DB_PASSWORD'),
#     'port': int(os.getenv('SOURCE_DB_PORT', 5432))
# }
#
# TARGET_DB_CONFIG = {
#     'host': os.getenv('TARGET_DB_HOST', 'localhost'),
#     'database': os.getenv('TARGET_DB_NAME', 'Footex_Api'),
#     'user': os.getenv('TARGET_DB_USER'),
#     'password': os.getenv('TARGET_DB_PASSWORD'),
#     'port': int(os.getenv('TARGET_DB_PORT', 5432))
# }
