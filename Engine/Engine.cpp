// Engine.cpp : Defines the functions for the shared library.

#include "pch.h"
#include "framework.h"
#include "Public/General/Shader.h"
#include <glad/glad.h>
#include <cstdio>
#include <chrono>
#include <math.h>
#include <unistd.h>
#include <string>
#include <glm.hpp>
#include <gtc/matrix_transform.hpp>
#include <gtc/type_ptr.hpp>
#include <memory>

#define STB_IMAGE_IMPLEMENTATION
#include <stb_image.h>

typedef void* (*GLADloadproc)(const char* name);

namespace {

GLuint g_VAO = 0;
GLuint g_VBO = 0;
GLuint g_EBO = 0;

std::unique_ptr<Shader> g_MainShader;
std::unique_ptr<Shader> g_DepthShader;

GLuint g_TextureID = 0;

glm::vec3 g_LightDir = glm::normalize(glm::vec3(0.3f, 1.0f, 0.2f));
glm::vec3 g_LightColor = glm::vec3(1.0f, 1.0f, 1.0f);
glm::vec3 g_ViewPos    = glm::vec3(0.0f);

std::string g_AssetRoot;

GLuint g_ShadowFBO = 0;
GLuint g_ShadowMap = 0;
const unsigned int SHADOW_WIDTH = 2048, SHADOW_HEIGHT = 2048;

glm::mat4 g_LightSpaceMatrix = glm::mat4(1.0f);

const char* vertexShaderPath        = "/Engine/Shaders/basic.vert";
const char* fragmentShaderPath      = "/Engine/Shaders/basic.frag";
const char* depthVertexShaderPath   = "/Engine/Shaders/depth.vert";
const char* depthFragmentShaderPath = "/Engine/Shaders/depth.frag";

void LoadTexture(const char* path)
{
    glGenTextures(1, &g_TextureID);
    glBindTexture(GL_TEXTURE_2D, g_TextureID);

    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

    stbi_set_flip_vertically_on_load(true);
    glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

    int width, height, channels;

    char cwd[1024];
    getcwd(cwd, sizeof(cwd));
    fprintf(stderr, "[Engine] CWD: %s\n", cwd);

    unsigned char* data = stbi_load(path, &width, &height, &channels, 0);
    if (data)
    {
        GLenum format = (channels == 4) ? GL_RGBA : GL_RGB;
        glTexImage2D(GL_TEXTURE_2D, 0, format, width, height, 0, format, GL_UNSIGNED_BYTE, data);
        glGenerateMipmap(GL_TEXTURE_2D);
    }
    else
        fprintf(stderr, "[Engine] Failed to load texture: %s\n", path);
    stbi_image_free(data);
}

void InitTriangle()
{
    float vertices[] = {
        // pos                  // uv           // normal
        // Front (z = 0)
        0.5f,  0.5f,  0.0f,    1.0f, 1.0f,      0.0f, 0.0f, 1.0f,
        0.5f, -0.5f,  0.0f,    1.0f, 0.0f,      0.0f, 0.0f, 1.0f,
        -0.5f,  0.5f,  0.0f,    0.0f, 1.0f,     0.0f, 0.0f, 1.0f,
        -0.5f, -0.5f,  0.0f,    0.0f, 0.0f,     0.0f, 0.0f, 1.0f,
        // Back (z = -1)
        -0.5f,  0.5f, -1.0f,    1.0f, 1.0f,     0.0f, 0.0f, -1.0f,
        -0.5f, -0.5f, -1.0f,    1.0f, 0.0f,     0.0f, 0.0f, -1.0f,
        0.5f,  0.5f, -1.0f,    0.0f, 1.0f,      0.0f, 0.0f, -1.0f,
        0.5f, -0.5f, -1.0f,    0.0f, 0.0f,      0.0f, 0.0f, -1.0f,
        // Left (x = -0.5)
        -0.5f,  0.5f,  0.0f,    1.0f, 1.0f,     -1.0f, 0.0f, 0.0f,
        -0.5f, -0.5f,  0.0f,    1.0f, 0.0f,     -1.0f, 0.0f, 0.0f,
        -0.5f,  0.5f, -1.0f,    0.0f, 1.0f,     -1.0f, 0.0f, 0.0f,
        -0.5f, -0.5f, -1.0f,    0.0f, 0.0f,     -1.0f, 0.0f, 0.0f,
        // Right (x = 0.5)
        0.5f,  0.5f, -1.0f,    1.0f, 1.0f,      1.0f, 0.0f, 0.0f,
        0.5f, -0.5f, -1.0f,    1.0f, 0.0f,      1.0f, 0.0f, 0.0f,
        0.5f,  0.5f,  0.0f,    0.0f, 1.0f,      1.0f, 0.0f, 0.0f,
        0.5f, -0.5f,  0.0f,    0.0f, 0.0f,      1.0f, 0.0f, 0.0f,
        // Top (y = 0.5)
        -0.5f,  0.5f, -1.0f,    0.0f, 1.0f,     0.0f, 1.0f, 0.0f,
        0.5f,  0.5f, -1.0f,    1.0f, 1.0f,      0.0f, 1.0f, 0.0f,
        -0.5f,  0.5f,  0.0f,    0.0f, 0.0f,     0.0f, 1.0f, 0.0f,
        0.5f,  0.5f,  0.0f,    1.0f, 0.0f,      0.0f, 1.0f, 0.0f,
        // Bottom (y = -0.5)
        -0.5f, -0.5f,  0.0f,    0.0f, 1.0f,     0.0f, -1.0f, 0.0f,
        0.5f, -0.5f,  0.0f,    1.0f, 1.0f,      0.0f, -1.0f, 0.0f,
        -0.5f, -0.5f, -1.0f,    0.0f, 0.0f,     0.0f, -1.0f, 0.0f,
        0.5f, -0.5f, -1.0f,    1.0f, 0.0f,      0.0f, -1.0f, 0.0f,
    };

    unsigned int indices[] = {
        0, 1, 2,        1, 3, 2,      // front
        4, 5, 6,        5, 7, 6,      // back
        8, 9, 10,       9, 11, 10,    // left
        12, 13, 14,     13, 15, 14,  // right
        16, 17, 18,     17, 19, 18,  // top
        20, 21, 22,     21, 23, 22,  // bottom
    };

    glGenBuffers(1, &g_EBO);

    glGenVertexArrays(1, &g_VAO);
    glGenBuffers(1, &g_VBO);


    glBindVertexArray(g_VAO);
    
    glBindBuffer(GL_ARRAY_BUFFER, g_VBO);
    glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);
    
    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, g_EBO);
    glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(indices), indices, GL_STATIC_DRAW);
    
    // Pos
    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 8 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(0);

    // UV
    glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 8 * sizeof(float), (void*)(3 * sizeof(float)));
    glEnableVertexAttribArray(1);

    // Normals
    glVertexAttribPointer(2, 3, GL_FLOAT, GL_FALSE, 8 * sizeof(float), (void*)(5 * sizeof(float)));
    glEnableVertexAttribArray(2);

    glBindBuffer(GL_ARRAY_BUFFER, 0);
    glBindVertexArray(0);
}

void InitShadowMap()
{
    glGenFramebuffers(1, &g_ShadowFBO);

    glGenTextures(1, &g_ShadowMap);
    glBindTexture(GL_TEXTURE_2D, g_ShadowMap);
    glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, SHADOW_WIDTH, SHADOW_HEIGHT, 0, GL_DEPTH_COMPONENT, GL_FLOAT, nullptr);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_BORDER);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_BORDER);
    float borderColor[] = { 1.0f, 1.0f, 1.0f, 1.0f };
    glTexParameterfv(GL_TEXTURE_2D, GL_TEXTURE_BORDER_COLOR, borderColor);

    glBindFramebuffer(GL_FRAMEBUFFER, g_ShadowFBO);
    glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, g_ShadowMap, 0);
    glDrawBuffer(GL_NONE);
    glReadBuffer(GL_NONE);

    if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
        fprintf(stderr, "[Engine] Shadow FBO incomplete\n");

    glBindFramebuffer(GL_FRAMEBUFFER, 0);
}

void UpdateLightSpaceMatrix()
{
    float sceneRadius = 10.0f; // TODO: derive from real scene bounds once you have more objects

    glm::vec3 lightPos = g_LightDir * sceneRadius;
    glm::mat4 lightView = glm::lookAt(lightPos, glm::vec3(0.0f), glm::vec3(0.0f, 1.0f, 0.0f));
    glm::mat4 lightProj = glm::ortho(-sceneRadius, sceneRadius, -sceneRadius, sceneRadius, 0.1f, sceneRadius * 2.0f);

    g_LightSpaceMatrix = lightProj * lightView;
}

} // anonymous namespace

extern "C" {

void Engine_Shutdown()
{
    glDeleteBuffers(1, &g_EBO);
    glDeleteBuffers(1, &g_VBO);
    glDeleteVertexArrays(1, &g_VAO);
    glDeleteTextures(1, &g_TextureID);
    glDeleteTextures(1, &g_ShadowMap);
    glDeleteFramebuffers(1, &g_ShadowFBO);

    g_MainShader.reset();
    g_DepthShader.reset();
}

void Engine_Init(void* getProcAddress, const char* assetRoot)
{
    if (!gladLoadGLLoader((GLADloadproc)getProcAddress))
    {
        fprintf(stderr, "[Engine] Failed to initialize GLAD\n");
        return;
    }
    fprintf(stderr, "[Engine] GLAD initialized, GL version %s\n", glGetString(GL_VERSION));

    glEnable(GL_DEPTH_TEST);

    g_AssetRoot = assetRoot;   // move this up first

    std::string vertPath  = g_AssetRoot + vertexShaderPath;
    std::string fragPath  = g_AssetRoot + fragmentShaderPath;
    std::string dVertPath = g_AssetRoot + depthVertexShaderPath;
    std::string dFragPath = g_AssetRoot + depthFragmentShaderPath;

    g_MainShader  = std::make_unique<Shader>(vertPath.c_str(), fragPath.c_str());
    g_DepthShader = std::make_unique<Shader>(dVertPath.c_str(), dFragPath.c_str());

    InitTriangle();
    InitShadowMap();
    UpdateLightSpaceMatrix();

    std::string texturePath = g_AssetRoot + "/Engine/TestTextures/wall.jpg";
    LoadTexture(texturePath.c_str());
}

void Engine_RenderFrame(int fb, int width, int height, const float* viewProj, const float* model)
{
    // --- Pass 1: render depth from the light's POV ---
    glViewport(0, 0, SHADOW_WIDTH, SHADOW_HEIGHT);
    glBindFramebuffer(GL_FRAMEBUFFER, g_ShadowFBO);
    glClear(GL_DEPTH_BUFFER_BIT);

    glEnable(GL_CULL_FACE);
    glCullFace(GL_FRONT);

    g_DepthShader->use();
    g_DepthShader->setMat4("uLightSpaceMatrix", g_LightSpaceMatrix);
    g_DepthShader->setMat4("uModel", glm::make_mat4(model));

    glBindVertexArray(g_VAO);
    glDrawElements(GL_TRIANGLES, 36, GL_UNSIGNED_INT, 0);
    glBindVertexArray(0);

    glDisable(GL_CULL_FACE);

    // --- Pass 2: normal scene render, sampling the shadow map ---
    glBindFramebuffer(GL_FRAMEBUFFER, fb);
    glViewport(0, 0, width, height);

    glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    g_MainShader->use();
    g_MainShader->setMat4("uViewProj", glm::make_mat4(viewProj));
    g_MainShader->setMat4("uModel", glm::make_mat4(model));
    g_MainShader->setMat4("uLightSpaceMatrix", g_LightSpaceMatrix);
    g_MainShader->setVec3("uLightDir", g_LightDir);
    g_MainShader->setVec3("uLightColor", g_LightColor);
    g_MainShader->setVec3("uViewPos", g_ViewPos);
    g_MainShader->setVec4("vertexColor", glm::vec4(1.0f));
    g_MainShader->setVec4("ambientLightColor", glm::vec4(0.15f, 0.15f, 0.15f, 1.0f));

    glActiveTexture(GL_TEXTURE0);
    glBindTexture(GL_TEXTURE_2D, g_TextureID);
    g_MainShader->setInt("uTexture", 0);

    glActiveTexture(GL_TEXTURE1);
    glBindTexture(GL_TEXTURE_2D, g_ShadowMap);
    g_MainShader->setInt("uShadowMap", 1);

    glBindVertexArray(g_VAO);
    glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
    glDrawElements(GL_TRIANGLES, 36, GL_UNSIGNED_INT, 0);
    glBindVertexArray(0);
}

void Engine_SetLight(const float* lightDir, const float* lightColor, const float* viewPos)
{
    g_LightDir   = glm::normalize(glm::vec3(lightDir[0], lightDir[1], lightDir[2]));
    g_LightColor = glm::vec3(lightColor[0], lightColor[1], lightColor[2]);
    g_ViewPos    = glm::vec3(viewPos[0], viewPos[1], viewPos[2]);
    UpdateLightSpaceMatrix();
}

}

// TODO: This is an example of a library function
void fnEngine()
{
    
}