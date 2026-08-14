# CMake stuff
cmake_minimum_required(VERSION 3.10)
project(LumenXEngine)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)

# Export Commands
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

# GLAD is plain C, giving it a small target
add_library(glad STATIC Engine/vendor/glad/src/glad.c)
target_include_directories(glad PUBLIC Engine/vendor/glad/include)
set_target_properties(glad PROPERTIES POSITION_INDEPENDENT_CODE ON)

# OpenGL
find_package(OpenGL REQUIRED)

# Add your files
add_library(Engine SHARED 
    Engine/Engine.cpp 
    Engine/pch.cpp
    Engine/Private/Base.cpp
    Engine/Private/Components/Common/BaseComponent.cpp
    Engine/Private/Components/Common/Mesh.cpp
    Engine/Private/Components/Common/Model.cpp
    Engine/Private/Components/Common/TransfromComponent.cpp
    Engine/Private/Entities/Common/BaseEntity.cpp
)

# Assimp stuff
find_package(assimp REQUIRED)
target_link_libraries(Engine PRIVATE assimp::assimp)

# If you have include folders
target_include_directories(Engine PUBLIC  ${CMAKE_CURRENT_SOURCE_DIR}/Engine/..)
target_include_directories(Engine PUBLIC  ${CMAKE_CURRENT_SOURCE_DIR})
target_include_directories(Engine PUBLIC  ${CMAKE_CURRENT_SOURCE_DIR}/Engine/vendor/stb)
target_include_directories(Engine PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/Engine/vendor/glm)
target_link_libraries(Engine PUBLIC glad OpenGL::GL)