cmake_minimum_required(VERSION 3.10)
project(LumenXEngine)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)

# GLAD is plain C, giving it a small target
add_library(glad STATIC Engine/vendor/glad/src/glad.c)
target_include_directories(glad PUBLIC Engine/vendor/glad/include)

find_package(OpenGL REQUIRED)

# Add your files
add_library(Engine SHARED 
    Engine/Engine.cpp 
    Engine/pch.cpp
)

# If you have include folders
target_include_directories(Engine PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(Engine PUBLIC glad OpenGL::GL)