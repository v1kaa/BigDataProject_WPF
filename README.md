# Weather-Sales Analytics 

A comprehensive BigData analytics application that explores the correlation between weather conditions and sales performance. This project demonstrates modern data pipeline architecture with automated data ingestion, transformation, and interactive visualization capabilities.

## 🎯 Project Overview

This application analyzes the impact of weather conditions on sales data through a three-layer architecture, providing insights into how temperature and weather patterns influence business performance. Built as a final project for BigData course.

## 🏗️ Architecture

### 1. Data Ingestion Layer
- **Weather Data**: Automatically fetched from external weather API
- **Sales Data**: Loaded from CSV files
- **Storage**: Both data types are stored in SQL database for persistence and efficient querying

### 2. Data Transformation Layer
- **Data Cleaning**: One-click data cleaning functionality to remove:
  - Records with null values
  - Duplicate entries
- **Data Preparation**: Processed data stored in database, ready for analysis

### 3. Analytics Layer
- **Interactive Analysis**: User-selectable date ranges for focused analysis
- **Statistical Insights**: Comprehensive statistics and visualizations
- **Correlation Analysis**: Temperature vs sales relationship with R² calculations

## 📊 Features

### Sales by Weather Conditions
- Visual breakdown of sales performance across different weather conditions
- Detailed statistics including:
  - Total sales per weather condition
  - Transaction counts
  - Average sale values
- Support for 11+ weather conditions (overcast, light drizzle, rain, snow, etc.)

### Temperature vs Sales Analysis
- Scatter plot visualization of temperature impact on sales
- **Correlation Analysis**: R² coefficient calculation (-0.004 indicating weak correlation)
- **Optimal Temperature**: Identification of peak sales temperature (23.8°C)
- **Peak Sales**: Highest recorded sales value ($363.50)
- Temperature range analysis with categorization:
  - Freezing: -4.4°C - 5.0°C
  - Cold: 5.1°C - 15.0°C  
  - Cool: 15.1°C - 20.0°C
  - Warm: 20.1°C - 26.1°C

### Sales Over Time
- **Flexible Grouping**: View data by days, weeks, or months
- **Multiple Metrics**:
  - Sum of sales
  - Average sales
  - Maximum sales
  - Minimum sales
- Time series visualization with interactive line charts

## 🛠️ Technology Stack

- **Framework**: .NET Core
- **Frontend**: WPF (Windows Presentation Foundation)
- **Language**: C#
- **Database**: SQLite
- **Data Processing**: LINQ for data querying and transformation
- **APIs**: External weather API integration with HTTP clients
- **Data Formats**: CSV file processing capabilities

- ## 📊 Dashboard Screenshots

The application provides three main analysis views:

1. **Weather Conditions Analysis** - Bar charts showing sales distribution across weather types
2. **Temperature Correlation** - Scatter plots with trend lines and statistical measures
3. **Time Series Analysis** - Line charts with flexible grouping options
 ![image](https://github.com/user-attachments/assets/9d740970-d23e-4964-a518-d267f6a3c907)
![image](https://github.com/user-attachments/assets/4f40b09b-7705-4c04-87cb-f2d26b0f61af)
![image](https://github.com/user-attachments/assets/d6ef33f2-eda4-4fc3-868f-8f5d62f2dcc4)
![image](https://github.com/user-attachments/assets/5c87ec0a-865e-4af4-ac47-a216b5dc33ea)
![image](https://github.com/user-attachments/assets/8a950bab-0300-480f-9055-fa178e3974d9)






## 🚀 Installation

1. Go to the [Releases](https://github.com/v1kaa/BigDataProject_WPF/releases) section.

