cmake_minimum_required(VERSION 3.10)
project(LumenXEngine)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)

# Add your files
add_library(Engine SHARED 
    Engine.cpp 
    pch.cpp
)

# If you have include folders
target_include_directories(Engine PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
